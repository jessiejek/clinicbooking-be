namespace ClinicApp.Application.Features.Auth.Dtos;

public sealed record LoginRequestDto(
    string Email,
    string Password);
