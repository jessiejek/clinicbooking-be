using FluentValidation;
using ClinicApp.Application.Features.Auth.Dtos;

namespace ClinicApp.Application.Features.Auth.Validators;

public sealed class SocialLoginRequestDtoValidator : AbstractValidator<SocialLoginRequestDto>
{
    public SocialLoginRequestDtoValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty()
            .Must(provider => provider is not null
                && (provider.Equals("Google", StringComparison.OrdinalIgnoreCase)
                    || provider.Equals("Facebook", StringComparison.OrdinalIgnoreCase)))
            .WithMessage("Provider must be either Google or Facebook.");

        When(x => x.Provider is not null && x.Provider.Equals("Google", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.IdToken)
                .NotEmpty()
                .WithMessage("IdToken is required for Google login.");
        });

        When(x => x.Provider is not null && x.Provider.Equals("Facebook", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.AccessToken)
                .NotEmpty()
                .WithMessage("AccessToken is required for Facebook login.");
        });
    }
}
