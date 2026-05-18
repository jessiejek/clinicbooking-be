using ClinicApp.Application.Features.Auth.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Auth.Validators;

public sealed class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}
