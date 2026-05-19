using ClinicApp.Application.Features.Services.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Services.Validators;

public sealed class UpdateServiceDtoValidator : AbstractValidator<UpdateServiceDto>
{
    private static readonly HashSet<string> AllowedCategories =
    [
        "Consultation",
        "Procedure",
        "Laboratory",
        "Diagnostic"
    ];

    public UpdateServiceDtoValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(200)
            .When(x => x.Name is not null);

        RuleFor(x => x.Description)
            .MaximumLength(4000)
            .When(x => x.Description is not null);

        RuleFor(x => x.Category)
            .Must(category => !string.IsNullOrWhiteSpace(category) && AllowedCategories.Contains(category.Trim()))
            .When(x => x.Category is not null)
            .WithMessage("Category must be Consultation, Procedure, Laboratory, or Diagnostic.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Price.HasValue);

        RuleFor(x => x.EstimatedDurationMinutes)
            .GreaterThan(0)
            .When(x => x.EstimatedDurationMinutes.HasValue);

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
