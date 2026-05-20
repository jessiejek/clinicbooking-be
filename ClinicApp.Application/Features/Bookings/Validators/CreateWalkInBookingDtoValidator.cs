using ClinicApp.Application.Features.Bookings.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Bookings.Validators;

public sealed class CreateWalkInBookingDtoValidator : AbstractValidator<CreateWalkInBookingDto>
{
    public CreateWalkInBookingDtoValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty();

        RuleFor(x => x.DoctorId)
            .NotEmpty();

        RuleFor(x => x.ServiceId)
            .NotEmpty();

        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .When(x => x.Notes is not null);
    }
}
