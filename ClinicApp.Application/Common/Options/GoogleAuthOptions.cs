namespace ClinicApp.Application.Common.Options;

public sealed class GoogleAuthOptions
{
    public const string SectionName = "Google";

    public string ClientId { get; init; } = string.Empty;
}
