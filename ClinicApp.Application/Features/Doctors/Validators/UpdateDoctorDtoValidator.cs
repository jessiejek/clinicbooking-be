using ClinicApp.Application.Features.Doctors.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Doctors.Validators;

public sealed class UpdateDoctorDtoValidator : AbstractValidator<UpdateDoctorDto>
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Active",
        "Inactive",
        "OnLeave"
    };

    public UpdateDoctorDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(200)
            .When(x => x.FullName is not null);

        RuleFor(x => x.Specialization)
            .NotEmpty()
            .MaximumLength(200)
            .When(x => x.Specialization is not null);

        RuleFor(x => x.Bio)
            .MaximumLength(4000)
            .When(x => x.Bio is not null);

        RuleFor(x => x.LicenseNumber)
            .MaximumLength(50)
            .When(x => x.LicenseNumber is not null);

        RuleFor(x => x.PtrNumber)
            .MaximumLength(50)
            .When(x => x.PtrNumber is not null);

        RuleFor(x => x.S2Number)
            .MaximumLength(50)
            .When(x => x.S2Number is not null);

        RuleFor(x => x.ConsultationFee)
            .GreaterThanOrEqualTo(0)
            .When(x => x.ConsultationFee.HasValue);

        RuleFor(x => x.SlotDurationMinutes)
            .GreaterThan(0)
            .When(x => x.SlotDurationMinutes.HasValue);

        RuleFor(x => x.SlotCapacity)
            .GreaterThan(0)
            .When(x => x.SlotCapacity.HasValue);

        RuleFor(x => x.DailyPatientLimit)
            .GreaterThan(0)
            .When(x => x.DailyPatientLimit.HasValue);

        RuleFor(x => x.Status)
            .MaximumLength(20)
            .When(x => !string.IsNullOrWhiteSpace(x.Status));

        RuleFor(x => x.Status)
            .Must(BeAllowedStatus)
            .When(x => !string.IsNullOrWhiteSpace(x.Status))
            .WithMessage("Status must be Active, Inactive, or OnLeave.");
    }

    private static bool BeAllowedStatus(string? status)
    {
        return string.IsNullOrWhiteSpace(status) || AllowedStatuses.Contains(status.Trim());
    }
}
