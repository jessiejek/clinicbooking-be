namespace ClinicApp.Application.Features.Auth.Dtos;

public sealed record ResetPasswordRequestDto(
    string Email,
    string Token,
    string NewPassword);
