namespace ClinicApp.Application.Features.Doctors.Dtos;

public sealed record DoctorScheduleInputDto(
    string DayOfWeek,
    string StartTime,
    string EndTime);
