namespace ClinicApp.Application.Features.Auth.Dtos;

public sealed record UpdateAuthProfileDto(
    string? FullName,
    string? AvatarUrl);
