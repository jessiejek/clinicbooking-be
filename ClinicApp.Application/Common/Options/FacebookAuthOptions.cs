namespace ClinicApp.Application.Common.Options;

public sealed class FacebookAuthOptions
{
    public const string SectionName = "Facebook";

    public string AppId { get; init; } = string.Empty;
    public string AppSecret { get; init; } = string.Empty;
    public string GraphApiVersion { get; init; } = "v19.0";
}
