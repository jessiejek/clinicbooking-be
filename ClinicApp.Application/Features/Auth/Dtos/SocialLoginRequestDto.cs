namespace ClinicApp.Application.Features.Auth.Dtos;

public sealed record SocialLoginRequestDto(
    string Provider,
    string? IdToken,
    string? AccessToken);
