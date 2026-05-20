namespace ClinicApp.Application.Features.Doctors.Dtos;

public sealed record DoctorScheduleDto(
    Guid Id,
    Guid DoctorId,
    string DayOfWeek,
    string StartTime,
    string EndTime);
