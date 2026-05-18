namespace ClinicApp.Application.Features.Auth.Dtos;

public sealed record RegisterPatientRequestDto(
    string FullName,
    string Email,
    string Password,
    string? AvatarUrl);
