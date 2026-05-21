using ClinicApp.Application.Features.Bookings.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Bookings.Validators;

public sealed class DoctorCompleteBookingDtoValidator : AbstractValidator<DoctorCompleteBookingDto>
{
    public DoctorCompleteBookingDtoValidator()
    {
        RuleFor(x => x.FinalAmount)
            .NotNull()
            .When(x => !x.IsProfessionalFeeWaived)
            .WithMessage("FinalAmount is required when professional fee is not waived.");

        RuleFor(x => x.FinalAmount)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.FinalAmount.HasValue)
            .WithMessage("FinalAmount must be greater than or equal to 0.");

        RuleFor(x => x.ProfessionalFeeWaivedReason)
            .NotEmpty()
            .When(x => x.IsProfessionalFeeWaived)
            .WithMessage("ProfessionalFeeWaivedReason is required when professional fee is waived.");

        RuleFor(x => x.ProfessionalFeeWaivedReason)
            .MaximumLength(500)
            .When(x => x.ProfessionalFeeWaivedReason is not null);

        RuleFor(x => x.SoapNotes)
            .MaximumLength(4000)
            .When(x => x.SoapNotes is not null);

        RuleFor(x => x.DoctorFeeNotes)
            .MaximumLength(2000)
            .When(x => x.DoctorFeeNotes is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .When(x => x.Notes is not null);
    }
}
