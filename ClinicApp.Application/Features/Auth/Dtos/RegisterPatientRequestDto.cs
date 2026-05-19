namespace ClinicApp.Application.Features.Auth.Dtos;

public sealed record RegisterPatientRequestDto(
    string FirstName,
    string? MiddleName,
    string LastName,
    string Email,
    string Password,
    string? AvatarUrl);
