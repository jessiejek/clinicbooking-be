using ClinicApp.Application.Features.Doctors.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Doctors.Validators;

public sealed class BlockDateDtoValidator : AbstractValidator<BlockDateDto>
{
    public BlockDateDtoValidator()
    {
        RuleFor(x => x.BlockedDate)
            .NotEmpty();

        RuleFor(x => x.Reason)
            .MaximumLength(300)
            .When(x => x.Reason is not null);
    }
}
