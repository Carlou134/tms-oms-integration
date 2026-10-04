using Microsoft.Extensions.Logging;
using TmsOmsIntegration.Application.Abstractions.Messaging;
using TmsOmsIntegration.Application.Abstractions.Notifications;
using TmsOmsIntegration.Application.Abstractions.Persistence;
using TmsOmsIntegration.Application.TmsEvents;
using TmsOmsIntegration.Domain.Orders.Events;

namespace TmsOmsIntegration.Application.Notifications;

public sealed class NotifyClientHandler(
    IOrderRepository orderRepository,
    NotificationFormatterResolver formatterResolver,
    INotificationSender notificationSender,
    ILogger<NotifyClientHandler> logger) : IEventHandler<TmsEventProcessed>
{
    public async Task HandleAsync(TmsEventProcessed message, CancellationToken cancellationToken = default)
    {
        var changes = message.DomainEvents.OfType<OrderStatusChanged>().ToList();

        if (changes.Count == 0)
            return;

        var tmsEvent = message.Event;
        var order = await orderRepository.GetByOrderNumberAsync(tmsEvent.OrderNumber, cancellationToken);

        // The OMS is the source of truth for clients; the TMS value is only a fallback.
        var clientCode = order?.ClientCode ?? tmsEvent.ClientCode;

        if (clientCode is null)
        {
            logger.LogWarning("No client code for order {OrderNumber}, notification skipped", tmsEvent.OrderNumber);
            return;
        }

        var formatter = formatterResolver.Resolve(clientCode);

        foreach (var change in changes)
        {
            var notification = new OrderStatusNotification(
                change.OrderNumber,
                tmsEvent.TrackingNumber,
                clientCode,
                tmsEvent.ClientName,
                change.NewStatus,
                change.IsAutomatic ? null : tmsEvent.SubStatus,
                change.VisitCount,
                change.IsAutomatic ? null : tmsEvent.ReceivedBy,
                change.IsAutomatic,
                change.EventDate);

            await notificationSender.SendAsync(formatter.Format(notification), cancellationToken);
        }
    }
}
