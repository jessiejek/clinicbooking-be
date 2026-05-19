namespace ClinicApp.Application.Features.Doctors.Dtos;

public sealed record DoctorBlockedDateDto(
    Guid Id,
    DateOnly BlockedDate,
    string? Reason);
