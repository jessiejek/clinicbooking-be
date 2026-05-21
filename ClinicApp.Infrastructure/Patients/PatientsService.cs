using System.Globalization;
using System.Net;
using System.Security.Claims;
using ClinicApp.Application.Common.Exceptions;
using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Common.Models;
using ClinicApp.Application.Features.Patients.Dtos;
using ClinicApp.Domain.Entities.Clinic;
using ClinicApp.Infrastructure.Identity;
using ClinicApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Infrastructure.Patients;

public sealed class PatientsService : IClinicPatientsService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IClinicRealtimeNotifier _realtimeNotifier;

    public PatientsService(AppDbContext dbContext, UserManager<ApplicationUser> userManager, IClinicRealtimeNotifier realtimeNotifier)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<PagedResult<PatientSummaryDto>> GetPatientsAsync(int page, int pageSize, string? search, CancellationToken cancellationToken)
    {
        var normalizedPage = NormalizePage(page);
        var normalizedPageSize = NormalizePageSize(pageSize);
        var trimmedSearch = TrimOrNull(search);

        var query = _dbContext.Patients.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(trimmedSearch))
        {
            query = query.Where(x =>
                x.PatientCode.Contains(trimmedSearch) ||
                x.FirstName.Contains(trimmedSearch) ||
                x.LastName.Contains(trimmedSearch) ||
                ((x.FirstName + " " + x.LastName).Contains(trimmedSearch)) ||
                (x.Email != null && x.Email.Contains(trimmedSearch)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ThenBy(x => x.PatientCode)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(x => new PatientSummaryDto(
                x.Id,
                x.PatientCode,
                x.FirstName,
                x.MiddleName,
                x.LastName,
                x.FirstName + " " + x.LastName,
                x.DateOfBirth,
                x.Sex,
                x.ContactNumber,
                x.Email,
                x.UserId,
                !string.IsNullOrWhiteSpace(x.UserId),
                x.IsGuest))
            .ToListAsync(cancellationToken);

        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)normalizedPageSize);
        return new PagedResult<PatientSummaryDto>(items, total, normalizedPage, normalizedPageSize, totalPages);
    }

    public async Task<PatientDetailDto> GetPatientAsync(Guid id, CancellationToken cancellationToken)
    {
        var patient = await LoadPatientAsync(id, cancellationToken);
        return Map(patient);
    }

    public async Task<PatientDetailDto> CreatePatientAsync(CreatePatientDto dto, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

        try
        {
            var trimmedUserId = TrimOrNull(dto.UserId);
            if (trimmedUserId is not null)
            {
                await EnsureLinkedUserExistsAsync(trimmedUserId, cancellationToken);
                await EnsureUserIsNotAlreadyLinkedAsync(trimmedUserId, cancellationToken);
            }

            var now = DateTime.UtcNow;
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                PatientCode = await GeneratePatientCodeAsync(now.Year, cancellationToken),
                UserId = trimmedUserId,
                FirstName = dto.FirstName.Trim(),
                MiddleName = TrimOrNull(dto.MiddleName),
                LastName = dto.LastName.Trim(),
                DateOfBirth = dto.DateOfBirth,
                Sex = NormalizeSex(dto.Sex),
                CivilStatus = TrimOrNull(dto.CivilStatus),
                Address = TrimOrNull(dto.Address),
                City = TrimOrNull(dto.City),
                ZipCode = TrimOrNull(dto.ZipCode),
                ContactNumber = TrimOrNull(dto.ContactNumber),
                Email = TrimOrNull(dto.Email),
                EmergencyContactName = TrimOrNull(dto.EmergencyContactName),
                EmergencyContactNumber = TrimOrNull(dto.EmergencyContactNumber),
                EmergencyContactRelationship = TrimOrNull(dto.EmergencyContactRelationship),
                BloodType = TrimOrNull(dto.BloodType),
                PhilHealthNumber = TrimOrNull(dto.PhilHealthNumber),
                HmoProvider = TrimOrNull(dto.HmoProvider),
                HmoCardNumber = TrimOrNull(dto.HmoCardNumber),
                IsGuest = string.IsNullOrWhiteSpace(trimmedUserId),
                IsEmailVerified = false,
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.Patients.Add(patient);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await GetPatientAsync(patient.Id, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PatientDetailDto> UpdatePatientAsync(Guid id, UpdatePatientDto dto, CancellationToken cancellationToken)
    {
        var patient = await LoadPatientAsync(id, cancellationToken);
        return await UpdatePatientCoreAsync(patient, dto, allowUserIdChange: true, cancellationToken);
    }

    public async Task<PatientDetailDto> CreatePortalAccountAsync(Guid id, CreatePatientPortalAccountDto dto, CancellationToken cancellationToken)
    {
        var email = TrimOrNull(dto.Email);
        var temporaryPassword = TrimOrNull(dto.TemporaryPassword);

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ApiException(HttpStatusCode.BadRequest, "Email is required.");
        }

        if (string.IsNullOrWhiteSpace(temporaryPassword))
        {
            throw new ApiException(HttpStatusCode.BadRequest, "Temporary password is required.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            var patient = await LoadPatientAsync(id, cancellationToken);
            if (!string.IsNullOrWhiteSpace(patient.UserId))
            {
                throw new ApiException(HttpStatusCode.BadRequest, "Patient already has a portal account.");
            }

            var existingUser = await _userManager.FindByEmailAsync(email);
            existingUser ??= await _userManager.FindByNameAsync(email);
            if (existingUser is not null)
            {
                throw new ApiException(HttpStatusCode.BadRequest, "Email is already used by another account.");
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = BuildFullName(patient.FirstName, patient.MiddleName, patient.LastName),
                Role = "Patient",
                AvatarUrl = null,
                AuthProvider = "Local",
                IsActive = true,
                IsFirstLogin = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, temporaryPassword);
            if (!createResult.Succeeded)
            {
                throw new ApiException(HttpStatusCode.BadRequest, string.Join("; ", createResult.Errors.Select(x => x.Description)));
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "Patient");
            if (!roleResult.Succeeded)
            {
                throw new ApiException(HttpStatusCode.BadRequest, string.Join("; ", roleResult.Errors.Select(x => x.Description)));
            }

            patient.UserId = user.Id;
            patient.IsGuest = false;
            if (string.IsNullOrWhiteSpace(patient.Email))
            {
                patient.Email = email;
            }

            patient.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            await _realtimeNotifier.NotifyPatientProfileUpdatedAsync(patient.Id, cancellationToken);
            return Map(patient);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PatientDetailDto> GetMyPatientAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var patient = await LoadCurrentPatientAsync(principal, cancellationToken);
        return Map(patient);
    }

    public async Task<PatientDetailDto> UpdateMyPatientAsync(ClaimsPrincipal principal, UpdatePatientDto dto, CancellationToken cancellationToken)
    {
        var patient = await LoadCurrentPatientAsync(principal, cancellationToken);
        return await UpdatePatientCoreAsync(patient, dto, allowUserIdChange: false, cancellationToken);
    }

    public async Task<PatientDetailDto> ConsentAsync(ClaimsPrincipal principal, ConsentDto dto, CancellationToken cancellationToken)
    {
        var patient = await LoadCurrentPatientAsync(principal, cancellationToken);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            patient.ConsentedAt = DateTime.UtcNow;
            patient.ConsentVersion = dto.ConsentVersion.Trim();
            patient.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            await _realtimeNotifier.NotifyPatientProfileUpdatedAsync(patient.Id, cancellationToken);
            return Map(patient);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<PatientDetailDto> UpdatePatientCoreAsync(Patient patient, UpdatePatientDto dto, bool allowUserIdChange, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        try
        {
            var trimmedUserId = allowUserIdChange ? TrimOrNull(dto.UserId) : patient.UserId;

            if (allowUserIdChange && dto.UserId is not null)
            {
                if (trimmedUserId is not null)
                {
                    await EnsureLinkedUserExistsAsync(trimmedUserId, cancellationToken);
                    await EnsureUserIsNotAlreadyLinkedAsync(trimmedUserId, cancellationToken, patient.Id);
                }

                patient.UserId = trimmedUserId;
                patient.IsGuest = string.IsNullOrWhiteSpace(trimmedUserId);
            }

            if (dto.FirstName is not null)
            {
                patient.FirstName = dto.FirstName.Trim();
            }

            if (dto.MiddleName is not null)
            {
                patient.MiddleName = TrimOrNull(dto.MiddleName);
            }

            if (dto.LastName is not null)
            {
                patient.LastName = dto.LastName.Trim();
            }

            if (dto.DateOfBirth.HasValue)
            {
                patient.DateOfBirth = dto.DateOfBirth.Value;
            }

            if (dto.Sex is not null)
            {
                patient.Sex = NormalizeSex(dto.Sex);
            }

            if (dto.CivilStatus is not null)
            {
                patient.CivilStatus = TrimOrNull(dto.CivilStatus);
            }

            if (dto.Address is not null)
            {
                patient.Address = TrimOrNull(dto.Address);
            }

            if (dto.City is not null)
            {
                patient.City = TrimOrNull(dto.City);
            }

            if (dto.ZipCode is not null)
            {
                patient.ZipCode = TrimOrNull(dto.ZipCode);
            }

            if (dto.ContactNumber is not null)
            {
                patient.ContactNumber = TrimOrNull(dto.ContactNumber);
            }

            if (dto.Email is not null)
            {
                patient.Email = TrimOrNull(dto.Email);
            }

            if (dto.EmergencyContactName is not null)
            {
                patient.EmergencyContactName = TrimOrNull(dto.EmergencyContactName);
            }

            if (dto.EmergencyContactNumber is not null)
            {
                patient.EmergencyContactNumber = TrimOrNull(dto.EmergencyContactNumber);
            }

            if (dto.EmergencyContactRelationship is not null)
            {
                patient.EmergencyContactRelationship = TrimOrNull(dto.EmergencyContactRelationship);
            }

            if (dto.BloodType is not null)
            {
                patient.BloodType = TrimOrNull(dto.BloodType);
            }

            if (dto.PhilHealthNumber is not null)
            {
                patient.PhilHealthNumber = TrimOrNull(dto.PhilHealthNumber);
            }

            if (dto.HmoProvider is not null)
            {
                patient.HmoProvider = TrimOrNull(dto.HmoProvider);
            }

            if (dto.HmoCardNumber is not null)
            {
                patient.HmoCardNumber = TrimOrNull(dto.HmoCardNumber);
            }

            patient.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            await _realtimeNotifier.NotifyPatientProfileUpdatedAsync(patient.Id, cancellationToken);
            return Map(patient);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<Patient> LoadPatientAsync(Guid id, CancellationToken cancellationToken)
    {
        var patient = await _dbContext.Patients.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (patient is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Patient was not found.");
        }

        return patient;
    }

    private async Task<Patient> LoadCurrentPatientAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(principal);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ApiException(HttpStatusCode.Unauthorized, "Unauthorized.");
        }

        var patient = await _dbContext.Patients.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (patient is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Patient was not found.");
        }

        return patient;
    }

    private async Task EnsureLinkedUserExistsAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Linked user account was not found.");
        }
    }

    private async Task EnsureUserIsNotAlreadyLinkedAsync(string userId, CancellationToken cancellationToken, Guid? excludePatientId = null)
    {
        var query = _dbContext.Patients.AsNoTracking().Where(x => x.UserId == userId);
        if (excludePatientId.HasValue)
        {
            query = query.Where(x => x.Id != excludePatientId.Value);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw new ApiException(HttpStatusCode.Conflict, "User already has a patient record.");
        }
    }

    private async Task<string> GeneratePatientCodeAsync(int year, CancellationToken cancellationToken)
    {
        var prefix = $"PT-{year}-";
        var latestCode = await _dbContext.Patients.AsNoTracking()
            .Where(x => x.PatientCode.StartsWith(prefix))
            .OrderByDescending(x => x.PatientCode)
            .Select(x => x.PatientCode)
            .FirstOrDefaultAsync(cancellationToken);

        var nextSequence = 1;
        if (!string.IsNullOrWhiteSpace(latestCode) && latestCode.Length >= prefix.Length + 5)
        {
            var sequencePart = latestCode[prefix.Length..];
            if (int.TryParse(sequencePart, NumberStyles.None, CultureInfo.InvariantCulture, out var currentSequence))
            {
                nextSequence = currentSequence + 1;
            }
        }

        if (nextSequence > 99999)
        {
            throw new ApiException(HttpStatusCode.Conflict, "Patient code sequence limit has been reached for this year.");
        }

        return $"{prefix}{nextSequence:D5}";
    }

    private static PatientDetailDto Map(Patient patient)
    {
        return new PatientDetailDto(
            Id: patient.Id,
            PatientCode: patient.PatientCode,
            UserId: patient.UserId,
            HasAccount: !string.IsNullOrWhiteSpace(patient.UserId),
            FirstName: patient.FirstName,
            MiddleName: patient.MiddleName,
            LastName: patient.LastName,
            DateOfBirth: patient.DateOfBirth,
            Sex: patient.Sex,
            CivilStatus: patient.CivilStatus,
            Address: patient.Address,
            City: patient.City,
            ZipCode: patient.ZipCode,
            ContactNumber: patient.ContactNumber,
            Email: patient.Email,
            EmergencyContactName: patient.EmergencyContactName,
            EmergencyContactNumber: patient.EmergencyContactNumber,
            EmergencyContactRelationship: patient.EmergencyContactRelationship,
            BloodType: patient.BloodType,
            PhilHealthNumber: patient.PhilHealthNumber,
            HmoProvider: patient.HmoProvider,
            HmoCardNumber: patient.HmoCardNumber,
            IsGuest: patient.IsGuest,
            IsEmailVerified: patient.IsEmailVerified,
            ConsentedAt: patient.ConsentedAt,
            ConsentVersion: patient.ConsentVersion,
            CreatedAt: patient.CreatedAt,
            UpdatedAt: patient.UpdatedAt,
            FullName: BuildFullName(patient.FirstName, patient.MiddleName, patient.LastName));
    }

    private static string BuildFullName(string firstName, string? middleName, string lastName)
    {
        var parts = new[] { firstName, middleName, lastName }
            .Select(part => part?.Trim())
            .Where(part => !string.IsNullOrWhiteSpace(part));

        return string.Join(" ", parts);
    }

    private static string NormalizeSex(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "male" => "Male",
            "female" => "Female",
            _ => value.Trim()
        };
    }

    private static string? TrimOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static int NormalizePage(int page)
    {
        return page < 1 ? 1 : page;
    }

    private static int NormalizePageSize(int pageSize)
    {
        if (pageSize <= 0)
        {
            return DefaultPageSize;
        }

        return Math.Min(pageSize, MaxPageSize);
    }
}
