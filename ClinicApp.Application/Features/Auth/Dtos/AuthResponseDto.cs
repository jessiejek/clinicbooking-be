namespace ClinicApp.Application.Features.Auth.Dtos;

public sealed record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    AuthUserDto User);
