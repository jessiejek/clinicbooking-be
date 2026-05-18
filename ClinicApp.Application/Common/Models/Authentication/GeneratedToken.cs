namespace ClinicApp.Application.Common.Models.Authentication;

public sealed record GeneratedToken(
    string AccessToken,
    DateTimeOffset ExpiresAt);
