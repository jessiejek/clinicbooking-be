namespace ClinicApp.Application.Features.Patients.Dtos;

public sealed record CreatePatientPortalAccountDto(
    string Email,
    string TemporaryPassword);
