using System.Globalization;
using System.Net;
using System.Security.Claims;
using ClinicApp.Application.Common.Exceptions;
using ClinicApp.Application.Common.Interfaces;
using ClinicApp.Application.Features.Doctors.Dtos;
using ClinicApp.Application.Features.Services.Dtos;
using ClinicApp.Domain.Entities.Clinic;
using ClinicApp.Infrastructure.Identity;
using ClinicApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Infrastructure.Doctors;

public sealed class DoctorsService : IClinicDoctorsService
{
    private const string DoctorRole = "Doctor";

    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public DoctorsService(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IReadOnlyList<DoctorSummaryDto>> GetActiveDoctorsAsync(CancellationToken cancellationToken)
    {
        return await QueryDoctorSummariesAsync(includeInactive: false, cancellationToken);
    }

    public async Task<IReadOnlyList<DoctorSummaryDto>> GetAllDoctorsAsync(CancellationToken cancellationToken)
    {
        return await QueryDoctorSummariesAsync(includeInactive: true, cancellationToken);
    }

    public async Task<DoctorDetailDto> GetDoctorDetailAsync(Guid id, bool includeInactive, CancellationToken cancellationToken)
    {
        var doctor = await _dbContext.Doctors
            .AsNoTracking()
            .Include(x => x.Schedules)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (doctor is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Doctor was not found.");
        }

        if (!includeInactive && !IsActiveDoctor(doctor))
        {
            throw new ApiException(HttpStatusCode.NotFound, "Doctor was not found.");
        }

        var blockedDates = await _dbContext.DoctorBlockedDates
            .AsNoTracking()
            .Where(x => x.DoctorId == id)
            .OrderBy(x => x.BlockedDate)
            .Select(x => new DoctorBlockedDateDto(
                x.Id,
                x.BlockedDate,
                x.Reason))
            .ToListAsync(cancellationToken);

        var todayStatus = await _dbContext.DoctorDayStatuses
            .AsNoTracking()
            .Where(x => x.DoctorId == id && x.Date == DateOnly.FromDateTime(DateTime.UtcNow))
            .Select(x => new DoctorDayStatusDto(
                x.Id,
                x.Date,
                x.Status,
                x.RunningLateMinutes))
            .SingleOrDefaultAsync(cancellationToken);

        var services = await LoadDoctorServicesAsync(id, includeInactive, cancellationToken);

        return MapDoctorDetail(doctor, blockedDates, services, todayStatus);
    }

    public async Task<DoctorDetailDto> GetMyDoctorAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var doctor = await GetCurrentDoctorAsync(principal, cancellationToken);
        return await GetDoctorDetailAsync(doctor.Id, includeInactive: true, cancellationToken);
    }

    public async Task<DoctorDetailDto> CreateDoctorAsync(CreateDoctorDto dto, CancellationToken cancellationToken)
    {
        await EnsureRoleExistsAsync(DoctorRole, cancellationToken);

        var existingUser = await _userManager.FindByEmailAsync(dto.DoctorEmail);
        if (existingUser is not null)
        {
            throw new ApiException(HttpStatusCode.Conflict, "Email address is already registered.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var now = DateTime.UtcNow;
            var user = new ApplicationUser
            {
                UserName = dto.DoctorEmail.Trim(),
                Email = dto.DoctorEmail.Trim(),
                EmailConfirmed = true,
                FullName = dto.FullName.Trim(),
                Role = DoctorRole,
                AuthProvider = "Local",
                IsActive = true,
                IsFirstLogin = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                throw new ApiException(HttpStatusCode.BadRequest, string.Join("; ", createResult.Errors.Select(x => x.Description)));
            }

            var passwordResult = await _userManager.AddPasswordAsync(user, dto.TempPassword);
            if (!passwordResult.Succeeded)
            {
                throw new ApiException(HttpStatusCode.BadRequest, string.Join("; ", passwordResult.Errors.Select(x => x.Description)));
            }

            var roleResult = await _userManager.AddToRoleAsync(user, DoctorRole);
            if (!roleResult.Succeeded)
            {
                throw new ApiException(HttpStatusCode.BadRequest, string.Join("; ", roleResult.Errors.Select(x => x.Description)));
            }

            var doctor = new Doctor
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                FullName = dto.FullName.Trim(),
                Specialization = dto.Specialization.Trim(),
                Bio = TrimOrNull(dto.Bio),
                LicenseNumber = TrimOrNull(dto.LicenseNumber),
                PtrNumber = TrimOrNull(dto.PtrNumber),
                S2Number = TrimOrNull(dto.S2Number),
                ConsultationFee = dto.ConsultationFee,
                SlotDurationMinutes = dto.SlotDurationMinutes,
                SlotCapacity = dto.SlotCapacity,
                DailyPatientLimit = dto.DailyPatientLimit,
                Status = "Active",
                ReviewCount = 0,
                CreatedAt = now,
                UpdatedAt = now
            };

            _dbContext.Doctors.Add(doctor);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await GetDoctorDetailAsync(doctor.Id, includeInactive: true, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<DoctorDetailDto> UpdateDoctorAsync(Guid id, UpdateDoctorDto dto, CancellationToken cancellationToken)
    {
        var doctor = await _dbContext.Doctors.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (doctor is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Doctor was not found.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            ApplyDoctorUpdates(doctor, dto);

            var user = await _userManager.FindByIdAsync(doctor.UserId);
            if (user is null)
            {
                throw new ApiException(HttpStatusCode.InternalServerError, "Linked doctor user account was not found.");
            }

            user.FullName = doctor.FullName;
            user.UpdatedAt = DateTime.UtcNow;

            var userResult = await _userManager.UpdateAsync(user);
            if (!userResult.Succeeded)
            {
                throw new ApiException(HttpStatusCode.BadRequest, string.Join("; ", userResult.Errors.Select(x => x.Description)));
            }

            doctor.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await GetDoctorDetailAsync(doctor.Id, includeInactive: true, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<DoctorDetailDto> UpdateMyDoctorAsync(ClaimsPrincipal principal, UpdateDoctorDto dto, CancellationToken cancellationToken)
    {
        var doctor = await GetCurrentDoctorAsync(principal, cancellationToken);
        return await UpdateDoctorAsync(doctor.Id, dto, cancellationToken);
    }

    public async Task DeleteDoctorAsync(Guid id, CancellationToken cancellationToken)
    {
        var doctor = await _dbContext.Doctors.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (doctor is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Doctor was not found.");
        }

        doctor.Status = "Inactive";
        doctor.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DoctorScheduleDto>> GetSchedulesAsync(Guid doctorId, CancellationToken cancellationToken)
    {
        var schedules = await _dbContext.DoctorSchedules
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId)
            .Select(x => new DoctorScheduleDto(
                x.Id,
                x.DoctorId,
                x.DayOfWeek,
                FormatTime(x.StartTime),
                FormatTime(x.EndTime)))
            .ToListAsync(cancellationToken);

        return schedules
            .OrderBy(x => GetDayOrder(x.DayOfWeek))
            .ThenBy(x => x.StartTime)
            .ToList();
    }

    public async Task<IReadOnlyList<DoctorScheduleDto>> UpsertSchedulesAsync(Guid doctorId, UpsertSchedulesDto dto, CancellationToken cancellationToken)
    {
        var doctor = await _dbContext.Doctors.SingleOrDefaultAsync(x => x.Id == doctorId, cancellationToken);
        if (doctor is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Doctor was not found.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var existingSchedules = await _dbContext.DoctorSchedules
                .Where(x => x.DoctorId == doctorId)
                .ToListAsync(cancellationToken);

            _dbContext.DoctorSchedules.RemoveRange(existingSchedules);

            var nowSchedules = new List<DoctorSchedule>();
            foreach (var schedule in dto.Schedules)
            {
                nowSchedules.Add(new DoctorSchedule
                {
                    Id = Guid.NewGuid(),
                    DoctorId = doctorId,
                    DayOfWeek = NormalizeDayOfWeek(schedule.DayOfWeek),
                    StartTime = ParseTime(schedule.StartTime),
                    EndTime = ParseTime(schedule.EndTime)
                });
            }

            _dbContext.DoctorSchedules.AddRange(nowSchedules);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var schedules = await _dbContext.DoctorSchedules
                .AsNoTracking()
                .Where(x => x.DoctorId == doctorId)
                .Select(x => new DoctorScheduleDto(
                    x.Id,
                    x.DoctorId,
                    x.DayOfWeek,
                    FormatTime(x.StartTime),
                    FormatTime(x.EndTime)))
                .ToListAsync(cancellationToken);

            return schedules
                .OrderBy(x => GetDayOrder(x.DayOfWeek))
                .ThenBy(x => x.StartTime)
                .ToList();
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<DoctorBlockedDateDto>> GetBlockedDatesAsync(Guid doctorId, CancellationToken cancellationToken)
    {
        await EnsureDoctorExistsAsync(doctorId, cancellationToken);

        return await _dbContext.DoctorBlockedDates
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId)
            .OrderBy(x => x.BlockedDate)
            .Select(x => new DoctorBlockedDateDto(
                x.Id,
                x.BlockedDate,
                x.Reason))
            .ToListAsync(cancellationToken);
    }

    public async Task<DoctorBlockedDateDto> UpsertBlockedDateAsync(Guid doctorId, BlockDateDto dto, CancellationToken cancellationToken)
    {
        await EnsureDoctorExistsAsync(doctorId, cancellationToken);

        var blockedDate = await _dbContext.DoctorBlockedDates
            .SingleOrDefaultAsync(x => x.DoctorId == doctorId && x.BlockedDate == dto.BlockedDate, cancellationToken);

        if (blockedDate is null)
        {
            blockedDate = new DoctorBlockedDate
            {
                Id = Guid.NewGuid(),
                DoctorId = doctorId,
                BlockedDate = dto.BlockedDate
            };
            _dbContext.DoctorBlockedDates.Add(blockedDate);
        }

        blockedDate.Reason = TrimOrNull(dto.Reason);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DoctorBlockedDateDto(
            Id: blockedDate.Id,
            BlockedDate: blockedDate.BlockedDate,
            Reason: blockedDate.Reason);
    }

    public async Task DeleteBlockedDateAsync(Guid doctorId, Guid blockedDateId, CancellationToken cancellationToken)
    {
        var blockedDate = await _dbContext.DoctorBlockedDates
            .SingleOrDefaultAsync(x => x.DoctorId == doctorId && x.Id == blockedDateId, cancellationToken);

        if (blockedDate is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Blocked date was not found.");
        }

        _dbContext.DoctorBlockedDates.Remove(blockedDate);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DoctorDayStatusDto>> GetDayStatusesAsync(Guid doctorId, CancellationToken cancellationToken)
    {
        await EnsureDoctorExistsAsync(doctorId, cancellationToken);

        return await _dbContext.DoctorDayStatuses
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId)
            .OrderBy(x => x.Date)
            .Select(x => new DoctorDayStatusDto(
                x.Id,
                x.Date,
                x.Status,
                x.RunningLateMinutes))
            .ToListAsync(cancellationToken);
    }

    public async Task<DoctorDayStatusDto> UpsertDayStatusAsync(Guid doctorId, SetDayStatusDto dto, CancellationToken cancellationToken)
    {
        await EnsureDoctorExistsAsync(doctorId, cancellationToken);

        var dayStatus = await _dbContext.DoctorDayStatuses
            .SingleOrDefaultAsync(x => x.DoctorId == doctorId && x.Date == dto.Date, cancellationToken);

        if (dayStatus is null)
        {
            dayStatus = new DoctorDayStatus
            {
                Id = Guid.NewGuid(),
                DoctorId = doctorId,
                Date = dto.Date
            };
            _dbContext.DoctorDayStatuses.Add(dayStatus);
        }

        dayStatus.Status = dto.Status;
        dayStatus.RunningLateMinutes = dto.Status.Equals("RunningLate", StringComparison.Ordinal)
            ? dto.RunningLateMinutes
            : null;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DoctorDayStatusDto(
            Id: dayStatus.Id,
            Date: dayStatus.Date,
            Status: dayStatus.Status,
            RunningLateMinutes: dayStatus.RunningLateMinutes);
    }

    public async Task<IReadOnlyList<AvailableSlotDto>> GetAvailableSlotsAsync(Guid doctorId, DateOnly date, CancellationToken cancellationToken)
    {
        var doctor = await _dbContext.Doctors
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == doctorId, cancellationToken);

        if (doctor is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Doctor was not found.");
        }

        if (!IsActiveDoctor(doctor))
        {
            return [];
        }

        var dayOfWeek = date.DayOfWeek.ToString();
        var schedules = await _dbContext.DoctorSchedules
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId && x.DayOfWeek == dayOfWeek)
            .ToListAsync(cancellationToken);

        if (schedules.Count == 0)
        {
            return [];
        }

        var isBlocked = await _dbContext.DoctorBlockedDates
            .AsNoTracking()
            .AnyAsync(x => x.DoctorId == doctorId && x.BlockedDate == date, cancellationToken);

        if (isBlocked)
        {
            return [];
        }

        var dayStatus = await _dbContext.DoctorDayStatuses
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.DoctorId == doctorId && x.Date == date, cancellationToken);

        if (dayStatus is not null && dayStatus.Status.Equals("UnavailableToday", StringComparison.Ordinal))
        {
            return [];
        }

        var candidateSlots = new List<(TimeOnly Start, TimeOnly End)>();
        foreach (var schedule in schedules)
        {
            var currentStart = schedule.StartTime;
            while (currentStart < schedule.EndTime)
            {
                var currentEnd = currentStart.AddMinutes(doctor.SlotDurationMinutes);
                if (currentEnd > schedule.EndTime)
                {
                    break;
                }

                candidateSlots.Add((currentStart, currentEnd));
                currentStart = currentEnd;
            }
        }

        var confirmedSlots = candidateSlots
            .Distinct()
            .OrderBy(x => x.Start)
            .ThenBy(x => x.End)
            .ToList();

        var totalConfirmedAndPending = 0;
        var dailyLimitReached = doctor.DailyPatientLimit.HasValue && totalConfirmedAndPending >= doctor.DailyPatientLimit.Value;
        var bookedCount = 0;

        return confirmedSlots
            .Select(slot => new AvailableSlotDto(
                FormatTime(slot.Start),
                FormatTime(slot.End),
                !dailyLimitReached && bookedCount < doctor.SlotCapacity,
                bookedCount,
                doctor.SlotCapacity))
            .ToList();
    }

    private async Task<IReadOnlyList<DoctorSummaryDto>> QueryDoctorSummariesAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var query = _dbContext.Doctors.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(x => x.Status == "Active");
        }

        return await query
            .OrderBy(x => x.FullName)
            .Select(x => new DoctorSummaryDto(
                x.Id,
                x.FullName,
                x.Specialization,
                x.ConsultationFee,
                x.AverageRating,
                x.ReviewCount,
                x.Status,
                x.ProfilePhotoUrl,
                x.UserId))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ServiceDto>> LoadDoctorServicesAsync(Guid doctorId, bool includeInactive, CancellationToken cancellationToken)
    {
        var query = from link in _dbContext.DoctorServices.AsNoTracking()
                    join service in _dbContext.Services.AsNoTracking() on link.ServiceId equals service.Id
                    where link.DoctorId == doctorId
                    select service;

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query
            .OrderBy(x => x.Name)
            .Select(x => new ServiceDto(
                x.Id,
                x.Name,
                x.Description,
                x.Category,
                x.Price,
                x.EstimatedDurationMinutes,
                x.IsActive))
            .ToListAsync(cancellationToken);
    }

    private async Task<Doctor> GetCurrentDoctorAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(principal);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ApiException(HttpStatusCode.Unauthorized, "Unauthorized.");
        }

        var doctor = await _dbContext.Doctors
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (doctor is null)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Doctor was not found.");
        }

        return doctor;
    }

    private async Task EnsureDoctorExistsAsync(Guid doctorId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Doctors.AnyAsync(x => x.Id == doctorId, cancellationToken);
        if (!exists)
        {
            throw new ApiException(HttpStatusCode.NotFound, "Doctor was not found.");
        }
    }

    private async Task EnsureRoleExistsAsync(string roleName, CancellationToken cancellationToken)
    {
        if (await _roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
        if (!result.Succeeded)
        {
            throw new ApiException(HttpStatusCode.BadRequest, string.Join("; ", result.Errors.Select(x => x.Description)));
        }
    }

    private static DoctorDetailDto MapDoctorDetail(
        Doctor doctor,
        IReadOnlyList<DoctorBlockedDateDto> blockedDates,
        IReadOnlyList<ServiceDto> services,
        DoctorDayStatusDto? todayStatus)
    {
        return new DoctorDetailDto(
            Id: doctor.Id,
            UserId: doctor.UserId,
            FullName: doctor.FullName,
            Specialization: doctor.Specialization,
            Bio: doctor.Bio,
            ProfilePhotoUrl: doctor.ProfilePhotoUrl,
            LicenseNumber: doctor.LicenseNumber,
            PtrNumber: doctor.PtrNumber,
            S2Number: doctor.S2Number,
            ConsultationFee: doctor.ConsultationFee,
            SlotDurationMinutes: doctor.SlotDurationMinutes,
            SlotCapacity: doctor.SlotCapacity,
            DailyPatientLimit: doctor.DailyPatientLimit,
            Status: doctor.Status,
            AverageRating: doctor.AverageRating,
            ReviewCount: doctor.ReviewCount,
            CreatedAt: doctor.CreatedAt,
            UpdatedAt: doctor.UpdatedAt,
            Schedules: doctor.Schedules
                .OrderBy(x => GetDayOrder(x.DayOfWeek))
                .ThenBy(x => x.StartTime)
                .Select(x => new DoctorScheduleDto(
                    Id: x.Id,
                    DoctorId: x.DoctorId,
                    DayOfWeek: x.DayOfWeek,
                    StartTime: FormatTime(x.StartTime),
                    EndTime: FormatTime(x.EndTime)))
                .ToList(),
            BlockedDates: blockedDates,
            Services: services,
            TodayStatus: todayStatus);
    }

    private static void ApplyDoctorUpdates(Doctor doctor, UpdateDoctorDto dto)
    {
        doctor.FullName = dto.FullName.Trim();
        doctor.Specialization = dto.Specialization.Trim();
        doctor.Bio = TrimOrNull(dto.Bio);
        doctor.LicenseNumber = TrimOrNull(dto.LicenseNumber);
        doctor.PtrNumber = TrimOrNull(dto.PtrNumber);
        doctor.S2Number = TrimOrNull(dto.S2Number);
        doctor.ConsultationFee = dto.ConsultationFee;
        doctor.SlotDurationMinutes = dto.SlotDurationMinutes;
        doctor.SlotCapacity = dto.SlotCapacity;
        doctor.DailyPatientLimit = dto.DailyPatientLimit;

        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            doctor.Status = NormalizeDoctorStatus(dto.Status);
        }
    }

    private static bool IsActiveDoctor(Doctor doctor)
    {
        return doctor.Status == "Active";
    }

    private static string NormalizeDayOfWeek(string value)
    {
        return Enum.Parse<DayOfWeek>(value.Trim(), ignoreCase: true).ToString();
    }

    private static int GetDayOrder(string dayOfWeek)
    {
        return dayOfWeek.ToUpperInvariant() switch
        {
            "MONDAY" => 1,
            "TUESDAY" => 2,
            "WEDNESDAY" => 3,
            "THURSDAY" => 4,
            "FRIDAY" => 5,
            "SATURDAY" => 6,
            "SUNDAY" => 7,
            _ => 8
        };
    }

    private static TimeOnly ParseTime(string value)
    {
        if (!TimeOnly.TryParseExact(value.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            throw new ApiException(HttpStatusCode.BadRequest, $"Invalid time value '{value}'. Expected HH:mm.");
        }

        return time;
    }

    private static string FormatTime(TimeOnly value)
    {
        return value.ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    private static string? TrimOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeDoctorStatus(string value)
    {
        return value.Trim().ToUpperInvariant() switch
        {
            "ACTIVE" => "Active",
            "INACTIVE" => "Inactive",
            "ONLEAVE" => "OnLeave",
            _ => value.Trim()
        };
    }
}
