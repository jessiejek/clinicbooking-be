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

        RuleFor(x => x.SlotEndTime)
            .NotNull()
            .WithMessage("SlotEndTime is required when SlotStartTime is provided.")
            .When(x => x.SlotStartTime.HasValue);

        RuleFor(x => x.SlotStartTime)
            .NotNull()
            .WithMessage("SlotStartTime is required when SlotEndTime is provided.")
            .When(x => x.SlotEndTime.HasValue);

        RuleFor(x => x)
            .Must(x => x.SlotEndTime! > x.SlotStartTime!)
            .WithMessage("SlotEndTime must be after SlotStartTime.")
            .When(x => x.SlotStartTime.HasValue && x.SlotEndTime.HasValue);
    }
}
