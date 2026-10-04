using TmsOmsIntegration.Application.TmsEvents;
using TmsOmsIntegration.Domain.Orders;
using TmsOmsIntegration.Domain.Orders.Events;

namespace TmsOmsIntegration.Application.Evidences;

public static class EvidenceMilestoneFilter
{
    private static readonly HashSet<OrderStatus> Milestones =
    [
        OrderStatus.Collected,
        OrderStatus.NotCollected,
        OrderStatus.Delivered,
        OrderStatus.NotDelivered,
        OrderStatus.Returned,
        OrderStatus.NotReturned
    ];

    public static bool ShouldStore(TmsEventProcessed message) =>
        Milestones.Contains(message.Event.Status)
        && message.Event.Evidences.Count > 0
        && WasApplied(message);

    private static bool WasApplied(TmsEventProcessed message) =>
        message.DomainEvents
            .OfType<OrderStatusChanged>()
            .Any(changed => !changed.IsAutomatic && changed.NewStatus == message.Event.Status);
}
