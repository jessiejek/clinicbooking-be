using ClinicApp.Application.Features.Bookings.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Bookings.Validators;

public sealed class RefundPaymentDtoValidator : AbstractValidator<RefundPaymentDto>
{
    public RefundPaymentDtoValidator()
    {
        RuleFor(x => x.RefundReason)
            .NotEmpty()
            .MaximumLength(500);
    }
}
