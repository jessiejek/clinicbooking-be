namespace ClinicApp.Application.Features.Auth.Dtos;

public sealed record FacebookLoginRequestDto(
    string? AccessToken,
    string? UserId,
    string? Provider);
