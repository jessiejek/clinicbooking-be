using ClinicApp.Application.Features.Bookings.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Bookings.Validators;

public sealed class CreateBookingDtoValidator : AbstractValidator<CreateBookingDto>
{
    private static readonly HashSet<string> AllowedPaymentModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Online",
        "PayAtClinic"
    };

    public CreateBookingDtoValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty();

        RuleFor(x => x.DoctorId)
            .NotEmpty();

        RuleFor(x => x.ServiceId)
            .NotEmpty();

        RuleFor(x => x.AppointmentDate)
            .Must(BeTodayOrFuture)
            .WithMessage("AppointmentDate must be today or in the future.");

        RuleFor(x => x.SlotStartTime)
            .NotEmpty();

        RuleFor(x => x.SlotEndTime)
            .NotEmpty()
            .Must((dto, end) => end > dto.SlotStartTime)
            .WithMessage("SlotEndTime must be after SlotStartTime.");

        RuleFor(x => x.PaymentMode)
            .NotEmpty()
            .MaximumLength(20)
            .Must(BeAllowedPaymentMode)
            .WithMessage("PaymentMode must be Online or PayAtClinic.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .When(x => x.Notes is not null);
    }

    private static bool BeTodayOrFuture(DateOnly value)
    {
        return value >= DateOnly.FromDateTime(DateTime.UtcNow.AddHours(8));
    }

    private static bool BeAllowedPaymentMode(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && AllowedPaymentModes.Contains(value.Trim());
    }
}
