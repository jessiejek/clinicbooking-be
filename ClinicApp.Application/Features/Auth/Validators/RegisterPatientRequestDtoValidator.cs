using ClinicApp.Application.Features.Auth.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Auth.Validators;

public sealed class RegisterPatientRequestDtoValidator : AbstractValidator<RegisterPatientRequestDto>
{
    public RegisterPatientRequestDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MiddleName).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.MiddleName));
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.AvatarUrl).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl));
    }
}
