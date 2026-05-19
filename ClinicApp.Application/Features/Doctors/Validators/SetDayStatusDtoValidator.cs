using ClinicApp.Application.Features.Doctors.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Doctors.Validators;

public sealed class SetDayStatusDtoValidator : AbstractValidator<SetDayStatusDto>
{
    private static readonly HashSet<string> AllowedStatuses =
    [
        "Available",
        "RunningLate",
        "UnavailableToday"
    ];

    public SetDayStatusDtoValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty();

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(status => !string.IsNullOrWhiteSpace(status) && AllowedStatuses.Contains(status.Trim()))
            .WithMessage("Status must be Available, RunningLate, or UnavailableToday.");

        RuleFor(x => x.RunningLateMinutes)
            .GreaterThanOrEqualTo(0)
            .When(x => x.RunningLateMinutes.HasValue);
    }
}
