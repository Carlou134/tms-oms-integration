namespace TmsOmsIntegration.Api.Webhooks.Tms;

public sealed class TmsWebhookOptions
{
    public const string SectionName = "TmsWebhook";

    public string ApiKey { get; init; } = string.Empty;
}
