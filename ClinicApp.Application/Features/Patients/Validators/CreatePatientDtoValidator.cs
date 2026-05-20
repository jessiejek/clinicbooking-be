using ClinicApp.Application.Features.Patients.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Patients.Validators;

public sealed class CreatePatientDtoValidator : AbstractValidator<CreatePatientDto>
{
    private static readonly HashSet<string> AllowedSexes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Male",
        "Female"
    };

    public CreatePatientDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.MiddleName)
            .MaximumLength(100)
            .When(x => x.MiddleName is not null);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.DateOfBirth)
            .Must(BeInThePast)
            .WithMessage("DateOfBirth must be in the past.");

        RuleFor(x => x.Sex)
            .NotEmpty()
            .MaximumLength(10)
            .Must(BeAllowedSex)
            .WithMessage("Sex must be Male or Female.");

        RuleFor(x => x.CivilStatus)
            .MaximumLength(20)
            .When(x => x.CivilStatus is not null);

        RuleFor(x => x.Address)
            .MaximumLength(300)
            .When(x => x.Address is not null);

        RuleFor(x => x.City)
            .MaximumLength(100)
            .When(x => x.City is not null);

        RuleFor(x => x.ZipCode)
            .MaximumLength(10)
            .When(x => x.ZipCode is not null);

        RuleFor(x => x.ContactNumber)
            .Matches(@"^\d{7,15}$")
            .When(x => x.ContactNumber is not null)
            .WithMessage("ContactNumber must be 7 to 15 digits.");

        RuleFor(x => x.Email)
            .MaximumLength(200)
            .EmailAddress()
            .When(x => x.Email is not null);

        RuleFor(x => x.EmergencyContactName)
            .MaximumLength(200)
            .When(x => x.EmergencyContactName is not null);

        RuleFor(x => x.EmergencyContactNumber)
            .MaximumLength(20)
            .Matches(@"^\d{7,15}$")
            .When(x => x.EmergencyContactNumber is not null)
            .WithMessage("EmergencyContactNumber must be 7 to 15 digits.");

        RuleFor(x => x.EmergencyContactRelationship)
            .MaximumLength(50)
            .When(x => x.EmergencyContactRelationship is not null);

        RuleFor(x => x.BloodType)
            .MaximumLength(5)
            .When(x => x.BloodType is not null);

        RuleFor(x => x.PhilHealthNumber)
            .MaximumLength(20)
            .When(x => x.PhilHealthNumber is not null);

        RuleFor(x => x.HmoProvider)
            .MaximumLength(100)
            .When(x => x.HmoProvider is not null);

        RuleFor(x => x.HmoCardNumber)
            .MaximumLength(50)
            .When(x => x.HmoCardNumber is not null);

        RuleFor(x => x.UserId)
            .NotEmpty()
            .MaximumLength(450)
            .When(x => x.UserId is not null);
    }

    private static bool BeInThePast(DateOnly date)
    {
        return date < DateOnly.FromDateTime(DateTime.UtcNow);
    }

    private static bool BeAllowedSex(string? sex)
    {
        return !string.IsNullOrWhiteSpace(sex) && AllowedSexes.Contains(sex.Trim());
    }
}
