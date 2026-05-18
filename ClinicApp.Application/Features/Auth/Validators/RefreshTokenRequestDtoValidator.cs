using ClinicApp.Application.Features.Auth.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Auth.Validators;

public sealed class RefreshTokenRequestDtoValidator : AbstractValidator<RefreshTokenRequestDto>
{
    public RefreshTokenRequestDtoValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
