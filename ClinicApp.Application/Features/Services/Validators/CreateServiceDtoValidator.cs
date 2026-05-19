using ClinicApp.Application.Features.Services.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Services.Validators;

public sealed class CreateServiceDtoValidator : AbstractValidator<CreateServiceDto>
{
    private static readonly HashSet<string> AllowedCategories =
    [
        "Consultation",
        "Procedure",
        "Laboratory",
        "Diagnostic"
    ];

    public CreateServiceDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(4000)
            .When(x => x.Description is not null);

        RuleFor(x => x.Category)
            .NotEmpty()
            .Must(category => !string.IsNullOrWhiteSpace(category) && AllowedCategories.Contains(category.Trim()))
            .WithMessage("Category must be Consultation, Procedure, Laboratory, or Diagnostic.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.EstimatedDurationMinutes)
            .GreaterThan(0);

        RuleFor(x => x.DoctorIds)
            .Must(HaveUniqueValues)
            .When(x => x.DoctorIds is not null)
            .WithMessage("DoctorIds must not contain duplicates.");
    }

    private static bool HaveUniqueValues(List<Guid>? doctorIds)
    {
        return doctorIds is null || doctorIds.Distinct().Count() == doctorIds.Count;
    }
}
