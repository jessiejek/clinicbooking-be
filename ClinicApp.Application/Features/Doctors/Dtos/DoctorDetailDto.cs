using ClinicApp.Application.Features.Services.Dtos;

namespace ClinicApp.Application.Features.Doctors.Dtos;

public sealed record DoctorDetailDto(
    Guid Id,
    string UserId,
    string FullName,
    string Specialization,
    string? Bio,
    string? ProfilePhotoUrl,
    string? LicenseNumber,
    string? PtrNumber,
    string? S2Number,
    decimal ConsultationFee,
    int SlotDurationMinutes,
    int SlotCapacity,
    int? DailyPatientLimit,
    string Status,
    decimal? AverageRating,
    int ReviewCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<DoctorScheduleDto> Schedules,
    IReadOnlyList<DoctorBlockedDateDto> BlockedDates,
    IReadOnlyList<ServiceDto> Services,
    DoctorDayStatusDto? TodayStatus);
