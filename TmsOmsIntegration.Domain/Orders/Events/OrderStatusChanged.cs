using TmsOmsIntegration.Domain.Abstractions;

namespace TmsOmsIntegration.Domain.Orders.Events;

public sealed record OrderStatusChanged(
    string OrderNumber,
    OrderStatus? PreviousStatus,
    OrderStatus NewStatus,
    ServiceType Phase,
    int VisitCount,
    DateTimeOffset EventDate) : IDomainEvent;
