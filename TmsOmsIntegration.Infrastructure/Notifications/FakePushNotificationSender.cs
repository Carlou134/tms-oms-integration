using Microsoft.Extensions.Logging;
using TmsOmsIntegration.Application.Abstractions.Notifications;
using TmsOmsIntegration.Application.Notifications;

namespace TmsOmsIntegration.Infrastructure.Notifications;

/// <summary>
/// Stands in for a push provider (e.g. Azure Notification Hubs or Firebase Cloud Messaging)
/// by logging what would be sent.
/// </summary>
internal sealed class FakePushNotificationSender(ILogger<FakePushNotificationSender> logger) : INotificationSender
{
    public Task SendAsync(ClientNotification notification, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Push notification sent to client {ClientCode} ({ContentType}): {Payload}",
            notification.ClientCode, notification.ContentType, notification.Payload);

        return Task.CompletedTask;
    }
}
