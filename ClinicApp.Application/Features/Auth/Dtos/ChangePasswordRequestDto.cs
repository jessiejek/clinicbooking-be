namespace ClinicApp.Application.Features.Auth.Dtos;

public sealed record ChangePasswordRequestDto(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword);
