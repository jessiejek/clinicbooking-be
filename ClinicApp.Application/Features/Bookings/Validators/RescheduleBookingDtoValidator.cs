using ClinicApp.Application.Features.Bookings.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Bookings.Validators;

public sealed class RescheduleBookingDtoValidator : AbstractValidator<RescheduleBookingDto>
{
    public RescheduleBookingDtoValidator()
    {
        RuleFor(x => x.NewAppointmentDate)
            .NotEmpty()
            .Must(BeTodayOrFuture)
            .WithMessage("NewAppointmentDate must be today or in the future.");

        RuleFor(x => x.NewSlotStartTime)
            .NotEmpty();

        RuleFor(x => x.NewSlotEndTime)
            .NotEmpty()
            .Must((dto, end) => end > dto.NewSlotStartTime)
            .WithMessage("NewSlotEndTime must be after NewSlotStartTime.");
    }

    private static bool BeTodayOrFuture(DateOnly value)
    {
        return value >= DateOnly.FromDateTime(DateTime.UtcNow.AddHours(8));
    }
}
