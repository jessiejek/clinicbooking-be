using ClinicApp.Application.Features.Bookings.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Bookings.Validators;

public sealed class CreateBookingDtoValidator : AbstractValidator<CreateBookingDto>
{
    public CreateBookingDtoValidator()
    {
        RuleFor(x => x.DoctorId)
            .NotEmpty();

        RuleFor(x => x)
            .Must(HaveAtLeastOneService)
            .WithMessage("At least one service must be selected.");

        RuleFor(x => x.AppointmentDate)
            .Must(BeTodayOrFuture)
            .WithMessage("AppointmentDate must be today or in the future.");

        RuleFor(x => x.SlotStartTime)
            .NotEmpty();

        RuleFor(x => x.SlotEndTime)
            .NotEmpty()
            .Must((dto, end) => end > dto.SlotStartTime)
            .WithMessage("SlotEndTime must be after SlotStartTime.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .When(x => x.Notes is not null);
    }

    private static bool BeTodayOrFuture(DateOnly value)
    {
        return value >= DateOnly.FromDateTime(DateTime.UtcNow.AddHours(8));
    }

    private static bool HaveAtLeastOneService(CreateBookingDto dto)
    {
        if (dto.ServiceIds is { Count: > 0 })
        {
            return dto.ServiceIds.Any(serviceId => serviceId != Guid.Empty);
        }

        return dto.ServiceId.HasValue && dto.ServiceId.Value != Guid.Empty;
    }
}
