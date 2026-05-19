namespace ClinicApp.Application.Features.Doctors.Dtos;

public sealed record DoctorDayStatusDto(
    Guid Id,
    DateOnly Date,
    string Status,
    int? RunningLateMinutes);
