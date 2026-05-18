namespace ClinicApp.Application.Features.Auth.Dtos;

public sealed record AuthUserDto(
    string Id,
    string FullName,
    string Email,
    string Role,
    string? AvatarUrl,
    bool IsFirstLogin);
