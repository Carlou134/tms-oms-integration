using TmsOmsIntegration.Application.Abstractions.Messaging;
using TmsOmsIntegration.Application.Abstractions.Persistence;
using TmsOmsIntegration.Application.TmsEvents;
using TmsOmsIntegration.Domain.Abstractions;
using TmsOmsIntegration.Domain.Orders;
using TmsOmsIntegration.Domain.Orders.Events;

namespace TmsOmsIntegration.Application.History;

public sealed class RecordHistoryHandler(
    IOrderHistoryRepository historyRepository,
    TimeProvider timeProvider) : IEventHandler<TmsEventProcessed>
{
    public async Task HandleAsync(TmsEventProcessed message, CancellationToken cancellationToken = default)
    {
        var recordedAt = timeProvider.GetUtcNow();

        var entries = message.DomainEvents
            .Select(domainEvent => ToEntry(domainEvent, message.Event, recordedAt))
            .ToList();

        if (entries.Count > 0)
            await historyRepository.AppendAsync(entries, cancellationToken);
    }

    private static OrderHistoryEntry ToEntry(IDomainEvent domainEvent, TmsEvent tmsEvent, DateTimeOffset recordedAt) =>
        domainEvent switch
        {
            OrderStatusChanged changed => new OrderHistoryEntry
            {
                OrderNumber = changed.OrderNumber,
                Phase = changed.Phase,
                Status = changed.NewStatus,
                Outcome = HistoryOutcome.Applied,
                IsAutomatic = changed.IsAutomatic,
                VisitCount = changed.VisitCount,
                // An automatic TO BE RETURN was not reported by a courier, so it carries no field data.
                SubStatus = changed.IsAutomatic ? null : tmsEvent.SubStatus,
                CourierName = changed.IsAutomatic ? null : tmsEvent.CourierName,
                VehicleCode = changed.IsAutomatic ? null : tmsEvent.VehicleCode,
                EventDate = changed.EventDate,
                RecordedAt = recordedAt
            },
            OrderEventRejected rejected => new OrderHistoryEntry
            {
                OrderNumber = rejected.OrderNumber,
                Phase = tmsEvent.ServiceType,
                Status = rejected.RequestedStatus,
                Outcome = HistoryOutcome.Rejected,
                RejectionReason = rejected.Reason,
                SubStatus = tmsEvent.SubStatus,
                CourierName = tmsEvent.CourierName,
                VehicleCode = tmsEvent.VehicleCode,
                EventDate = rejected.EventDate,
                RecordedAt = recordedAt
            },
            _ => throw new NotSupportedException($"No history mapping for {domainEvent.GetType().Name}.")
        };
}
