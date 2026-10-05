using TmsOmsIntegration.Application.Abstractions.Notifications;
using TmsOmsIntegration.Application.Notifications;

namespace TmsOmsIntegration.Tests.Fakes;

internal sealed class StubNotificationFormatter(string? clientCode) : INotificationFormatter
{
    public string? ClientCode => clientCode;

    public ClientNotification Format(OrderStatusNotification notification) =>
        new(notification.ClientCode, "text/plain", $"Formatted by {clientCode ?? "default"}");
}
