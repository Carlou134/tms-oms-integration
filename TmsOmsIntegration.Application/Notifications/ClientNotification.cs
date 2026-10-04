namespace TmsOmsIntegration.Application.Notifications;

public sealed record ClientNotification(
    string ClientCode,
    string ContentType,
    string Payload);
