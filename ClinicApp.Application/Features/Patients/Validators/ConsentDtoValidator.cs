using ClinicApp.Application.Features.Patients.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Patients.Validators;

public sealed class ConsentDtoValidator : AbstractValidator<ConsentDto>
{
    public ConsentDtoValidator()
    {
        RuleFor(x => x.ConsentVersion)
            .NotEmpty()
            .MaximumLength(10);
    }
}
