using ClinicApp.Application.Features.Bookings.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Bookings.Validators;

public sealed class WaivePaymentDtoValidator : AbstractValidator<WaivePaymentDto>
{
    public WaivePaymentDtoValidator()
    {
        RuleFor(x => x.WaivedReason)
            .NotEmpty()
            .MaximumLength(500);
    }
}
