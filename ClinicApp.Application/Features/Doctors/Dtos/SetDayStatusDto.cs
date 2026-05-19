namespace ClinicApp.Application.Features.Doctors.Dtos;

public sealed record SetDayStatusDto(
    DateOnly Date,
    string Status,
    int? RunningLateMinutes);
