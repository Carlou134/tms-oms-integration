using TmsOmsIntegration.Application.Notifications;

namespace TmsOmsIntegration.Application.Abstractions.Notifications;

public interface INotificationSender
{
    Task SendAsync(ClientNotification notification, CancellationToken cancellationToken = default);
}
