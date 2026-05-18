using ClinicApp.Application.Features.Auth.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Auth.Validators;

public sealed class SetPasswordRequestDtoValidator : AbstractValidator<SetPasswordRequestDto>
{
    public SetPasswordRequestDtoValidator()
    {
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}
