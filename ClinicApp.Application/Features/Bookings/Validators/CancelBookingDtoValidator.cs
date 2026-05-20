using ClinicApp.Application.Features.Bookings.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Bookings.Validators;

public sealed class CancelBookingDtoValidator : AbstractValidator<CancelBookingDto>
{
    public CancelBookingDtoValidator()
    {
        RuleFor(x => x.CancellationReason)
            .NotEmpty()
            .MaximumLength(500);
    }
}
