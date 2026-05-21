using ClinicApp.Application.Features.Bookings.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Bookings.Validators;

public sealed class ConfirmClinicPaymentDtoValidator : AbstractValidator<ConfirmClinicPaymentDto>
{
    private static readonly HashSet<string> AllowedPaymentMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cash",
        "GCash",
        "Maya",
        "BankTransfer"
    };

    public ConfirmClinicPaymentDtoValidator()
    {
        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .MaximumLength(20)
            .Must(BeAllowedPaymentMethod)
            .WithMessage("PaymentMethod must be Cash, GCash, Maya, or BankTransfer.");

        RuleFor(x => x.AmountReceived)
            .GreaterThanOrEqualTo(0m);

        RuleFor(x => x.ReferenceNumber)
            .MaximumLength(100)
            .When(x => x.ReferenceNumber is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .When(x => x.Notes is not null);
    }

    private static bool BeAllowedPaymentMethod(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && AllowedPaymentMethods.Contains(value.Trim());
    }
}
