using ClinicApp.Application.Features.Bookings.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Bookings.Validators;

public sealed class SubmitProofDtoValidator : AbstractValidator<SubmitProofDto>
{
    private static readonly HashSet<string> AllowedProofTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ReferenceNumber",
        "Screenshot"
    };

    public SubmitProofDtoValidator()
    {
        RuleFor(x => x.ProofType)
            .NotEmpty()
            .MaximumLength(20)
            .Must(BeAllowedProofType)
            .WithMessage("ProofType must be ReferenceNumber or Screenshot.");

        RuleFor(x => x.ProofValue)
            .NotEmpty()
            .MaximumLength(500);
    }

    private static bool BeAllowedProofType(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && AllowedProofTypes.Contains(value.Trim());
    }
}
