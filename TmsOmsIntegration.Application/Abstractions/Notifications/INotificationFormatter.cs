using TmsOmsIntegration.Application.Notifications;

namespace TmsOmsIntegration.Application.Abstractions.Notifications;

public interface INotificationFormatter
{
    /// <summary>
    /// The client this formatter builds notifications for, or null for the default formatter.
    /// </summary>
    string? ClientCode { get; }

    ClientNotification Format(OrderStatusNotification notification);
}
