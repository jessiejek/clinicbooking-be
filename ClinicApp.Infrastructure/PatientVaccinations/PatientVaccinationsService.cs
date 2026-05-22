using System.Net;
using System.Security.Claims;
using ClinicApp.Application.Common.Exceptions;
using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Features.PatientVaccinations.Dtos;
using ClinicApp.Domain.Entities.Clinic;
using ClinicApp.Infrastructure.Identity;
using ClinicApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Infrastructure.PatientVaccinations;

public sealed class PatientVaccinationsService : IPatientVaccinationsService
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public PatientVaccinationsService(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<PatientVaccinationDto>> GetPatientVaccinationsAsync(
        Guid patientId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var patient = await LoadPatientForAccessAsync(patientId, principal, cancellationToken);
        return await GetVaccinationsCoreAsync(patient.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<PatientVaccinationDto>> GetMyVaccinationsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var patient = await LoadCurrentPatientAsync(principal, cancellationToken);
        return await GetVaccinationsCoreAsync(patient.Id, cancellationToken);
    }

    public async Task<PatientVaccinationDto> CreatePatientVaccinationAsync(
        Guid patientId,
        CreatePatientVaccinationDto dto,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var patient = await LoadPatientForAccessAsync(patientId, principal, cancellationToken);
        ValidateVaccinationDto(dto.VaccineName, dto.AdministeredDate, dto.Status, dto.Source);
        ValidateDateOrder(dto.AdministeredDate, dto.ExpirationDate, dto.NextDueDate);

        var now = DateTime.UtcNow;
        var userId = _userManager.GetUserId(principal) ?? string.Empty;

        var entity = new PatientVaccination
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            BookingId = dto.BookingId,
            ConsultationId = dto.ConsultationId,
            DoctorId = dto.DoctorId,
            AdministeredByUserId = dto.Source == "AdministeredInClinic" ? userId : null,
            VaccineName = dto.VaccineName.Trim(),
            VaccineCode = dto.VaccineCode?.Trim(),
            Manufacturer = dto.Manufacturer?.Trim(),
            LotNumber = dto.LotNumber?.Trim(),
            ExpirationDate = dto.ExpirationDate,
            AdministeredDate = dto.AdministeredDate,
            DoseNumber = dto.DoseNumber?.Trim(),
            DoseAmount = dto.DoseAmount,
            DoseUnit = dto.DoseUnit?.Trim(),
            Route = dto.Route?.Trim(),
            Site = dto.Site?.Trim(),
            Status = dto.Status,
            Source = dto.Source,
            NextDueDate = dto.NextDueDate,
            VisEditionDate = dto.VisEditionDate,
            VisProvidedDate = dto.VisProvidedDate,
            Notes = dto.Notes?.Trim(),
            ReactionNotes = dto.ReactionNotes?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = userId,
            UpdatedByUserId = userId
        };

        _dbContext.Set<PatientVaccination>().Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapVaccination(entity);
    }

    public async Task<PatientVaccinationDto> UpdatePatientVaccinationAsync(
        Guid patientId,
        Guid vaccinationId,
        UpdatePatientVaccinationDto dto,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        await LoadPatientForAccessAsync(patientId, principal, cancellationToken);
        ValidateVaccinationDto(dto.VaccineName, dto.AdministeredDate, dto.Status, dto.Source);
        ValidateDateOrder(dto.AdministeredDate, dto.ExpirationDate, dto.NextDueDate);

        var entity = await LoadVaccinationAsync(patientId, vaccinationId, cancellationToken);
        var userId = _userManager.GetUserId(principal) ?? string.Empty;

        entity.BookingId = dto.BookingId;
        entity.ConsultationId = dto.ConsultationId;
        entity.DoctorId = dto.DoctorId;
        entity.VaccineName = dto.VaccineName.Trim();
        entity.VaccineCode = dto.VaccineCode?.Trim();
        entity.Manufacturer = dto.Manufacturer?.Trim();
        entity.LotNumber = dto.LotNumber?.Trim();
        entity.ExpirationDate = dto.ExpirationDate;
        entity.AdministeredDate = dto.AdministeredDate;
        entity.DoseNumber = dto.DoseNumber?.Trim();
        entity.DoseAmount = dto.DoseAmount;
        entity.DoseUnit = dto.DoseUnit?.Trim();
        entity.Route = dto.Route?.Trim();
        entity.Site = dto.Site?.Trim();
        entity.Status = dto.Status;
        entity.Source = dto.Source;
        entity.NextDueDate = dto.NextDueDate;
        entity.VisEditionDate = dto.VisEditionDate;
        entity.VisProvidedDate = dto.VisProvidedDate;
        entity.Notes = dto.Notes?.Trim();
        entity.ReactionNotes = dto.ReactionNotes?.Trim();
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = userId;

        if (dto.Source == "AdministeredInClinic" && string.IsNullOrWhiteSpace(entity.AdministeredByUserId))
        {
            entity.AdministeredByUserId = userId;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapVaccination(entity);
    }

    public async Task DeletePatientVaccinationAsync(
        Guid patientId,
        Guid vaccinationId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        await LoadPatientForAccessAsync(patientId, principal, cancellationToken);
        var entity = await LoadVaccinationAsync(patientId, vaccinationId, cancellationToken);

        _dbContext.Set<PatientVaccination>().Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<PatientVaccinationDto>> GetVaccinationsCoreAsync(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        var vaccinations = await _dbContext.Set<PatientVaccination>().AsNoTracking()
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.AdministeredDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return vaccinations.Select(MapVaccination).ToList();
    }

    private async Task<Patient> LoadCurrentPatientAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(principal);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ApiException(HttpStatusCode.Unauthorized, "Unauthorized.");
        }

        var patient = await _dbContext.Patients.AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (patient is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Patient profile not found for current user.");
        }

        return patient;
    }

    private async Task<Patient> LoadPatientForAccessAsync(
        Guid patientId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (principal.IsInRole("Patient"))
        {
            var currentPatient = await LoadCurrentPatientAsync(principal, cancellationToken);
            if (currentPatient.Id != patientId)
            {
                throw new ApiException(HttpStatusCode.NotFound, "Patient was not found.");
            }
            return currentPatient;
        }

        var patient = await _dbContext.Patients.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == patientId, cancellationToken);

        if (patient is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Patient was not found.");
        }

        return patient;
    }

    private async Task<PatientVaccination> LoadVaccinationAsync(
        Guid patientId,
        Guid vaccinationId,
        CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Set<PatientVaccination>()
            .SingleOrDefaultAsync(x => x.Id == vaccinationId && x.PatientId == patientId, cancellationToken);

        if (entity is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Vaccination record was not found.");
        }

        return entity;
    }

    private static void ValidateVaccinationDto(
        string vaccineName,
        DateOnly administeredDate,
        string status,
        string source)
    {
        if (string.IsNullOrWhiteSpace(vaccineName))
        {
            throw new ApiException(HttpStatusCode.BadRequest, "Vaccine name is required.");
        }

        if (administeredDate == default)
        {
            throw new ApiException(HttpStatusCode.BadRequest, "Administered date is required.");
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ApiException(HttpStatusCode.BadRequest, "Status is required.");
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ApiException(HttpStatusCode.BadRequest, "Source is required.");
        }

        var allowedStatuses = new[] { "Completed", "NotDone", "EnteredInError" };
        if (!allowedStatuses.Contains(status))
        {
            throw new ApiException(HttpStatusCode.BadRequest,
                $"Status must be one of: {string.Join(", ", allowedStatuses)}.");
        }

        var allowedSources = new[] { "AdministeredInClinic", "Historical", "PatientReported", "ExternalRecord" };
        if (!allowedSources.Contains(source))
        {
            throw new ApiException(HttpStatusCode.BadRequest,
                $"Source must be one of: {string.Join(", ", allowedSources)}.");
        }
    }

    private static void ValidateDateOrder(DateOnly administeredDate, DateOnly? expirationDate, DateOnly? nextDueDate)
    {
        if (expirationDate.HasValue && expirationDate.Value < administeredDate)
        {
            throw new ApiException(HttpStatusCode.BadRequest,
                "Expiration date cannot be before the administered date.");
        }

        if (nextDueDate.HasValue && nextDueDate.Value < administeredDate)
        {
            throw new ApiException(HttpStatusCode.BadRequest,
                "Next due date cannot be before the administered date.");
        }
    }

    private static PatientVaccinationDto MapVaccination(PatientVaccination entity)
    {
        return new PatientVaccinationDto(
            Id: entity.Id,
            PatientId: entity.PatientId,
            BookingId: entity.BookingId,
            ConsultationId: entity.ConsultationId,
            DoctorId: entity.DoctorId,
            AdministeredByUserId: entity.AdministeredByUserId,
            VaccineName: entity.VaccineName,
            VaccineCode: entity.VaccineCode,
            Manufacturer: entity.Manufacturer,
            LotNumber: entity.LotNumber,
            ExpirationDate: entity.ExpirationDate,
            AdministeredDate: entity.AdministeredDate,
            DoseNumber: entity.DoseNumber,
            DoseAmount: entity.DoseAmount,
            DoseUnit: entity.DoseUnit,
            Route: entity.Route,
            Site: entity.Site,
            Status: entity.Status,
            Source: entity.Source,
            NextDueDate: entity.NextDueDate,
            VisEditionDate: entity.VisEditionDate,
            VisProvidedDate: entity.VisProvidedDate,
            Notes: entity.Notes,
            ReactionNotes: entity.ReactionNotes,
            CreatedAt: entity.CreatedAt,
            UpdatedAt: entity.UpdatedAt);
    }
}
