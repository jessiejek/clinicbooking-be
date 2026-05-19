using ClinicApp.Application.Features.Doctors.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Doctors.Validators;

public sealed class CreateDoctorDtoValidator : AbstractValidator<CreateDoctorDto>
{
    public CreateDoctorDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Specialization)
            .NotEmpty()
            .MaximumLength(200);

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
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.SlotDurationMinutes)
            .GreaterThan(0);

        RuleFor(x => x.SlotCapacity)
            .GreaterThan(0);

        RuleFor(x => x.DailyPatientLimit)
            .GreaterThan(0)
            .When(x => x.DailyPatientLimit.HasValue);

        RuleFor(x => x.DoctorEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.TempPassword)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128);
    }
}
