using Microsoft.Extensions.Logging;
using TmsOmsIntegration.Application.Abstractions.Messaging;
using TmsOmsIntegration.Application.Abstractions.Persistence;

namespace TmsOmsIntegration.Application.TmsEvents;

public sealed class ProcessTmsEventHandler(
    IOrderRepository orderRepository,
    IOutbox outbox,
    ILogger<ProcessTmsEventHandler> logger) : IEventHandler<TmsEventReceived>
{
    public async Task HandleAsync(TmsEventReceived message, CancellationToken cancellationToken = default)
    {
        var tmsEvent = message.Event;
        var order = await orderRepository.GetByOrderNumberAsync(tmsEvent.OrderNumber, cancellationToken);

        if (order is null)
        {
            logger.LogWarning("Order {OrderNumber} not found in OMS, {Status} event ignored",
                tmsEvent.OrderNumber, tmsEvent.Status);
            return;
        }

        order.Apply(tmsEvent.ServiceType, tmsEvent.Status, tmsEvent.EventDate);

        var processed = new TmsEventProcessed(message.EventKey, tmsEvent, [.. order.DomainEvents]);
        order.ClearDomainEvents();

        // With a database, both writes share one transaction committed by SaveAsync, so it goes last.
        await outbox.AddAsync(processed, cancellationToken);
        await orderRepository.SaveAsync(order, cancellationToken);
    }
}
