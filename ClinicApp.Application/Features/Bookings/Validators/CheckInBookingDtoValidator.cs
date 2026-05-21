using ClinicApp.Application.Features.Bookings.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Bookings.Validators;

public sealed class CheckInBookingDtoValidator : AbstractValidator<CheckInBookingDto>
{
    public CheckInBookingDtoValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .When(x => x.Notes is not null);
    }
}
