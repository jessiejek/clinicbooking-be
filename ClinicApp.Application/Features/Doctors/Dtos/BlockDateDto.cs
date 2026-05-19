namespace ClinicApp.Application.Features.Doctors.Dtos;

public sealed record BlockDateDto(
    DateOnly BlockedDate,
    string? Reason);
