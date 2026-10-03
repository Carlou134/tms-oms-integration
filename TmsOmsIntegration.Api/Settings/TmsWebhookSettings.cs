namespace TmsOmsIntegration.Api.Settings;

public sealed class TmsWebhookSettings
{
    public const string SectionName = "TmsWebhook";

    public string ApiKey { get; init; } = string.Empty;
}
