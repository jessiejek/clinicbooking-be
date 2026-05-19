namespace ClinicApp.Application.Features.Doctors.Dtos;

public sealed record DoctorScheduleDto(
    Guid Id,
    string DayOfWeek,
    string StartTime,
    string EndTime);
