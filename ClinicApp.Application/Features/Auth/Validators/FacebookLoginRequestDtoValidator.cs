using ClinicApp.Application.Features.Auth.Dtos;
using FluentValidation;

namespace ClinicApp.Application.Features.Auth.Validators;

public sealed class FacebookLoginRequestDtoValidator : AbstractValidator<FacebookLoginRequestDto>
{
    public FacebookLoginRequestDtoValidator()
    {
        RuleFor(x => x.AccessToken)
            .NotEmpty()
            .WithMessage("AccessToken is required for Facebook login.");

        When(x => !string.IsNullOrWhiteSpace(x.Provider), () =>
        {
            RuleFor(x => x.Provider)
                .Must(provider => provider!.Equals("Facebook", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Provider must be Facebook.");
        });
    }
}
