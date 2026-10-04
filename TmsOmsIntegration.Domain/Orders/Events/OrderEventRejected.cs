using TmsOmsIntegration.Domain.Abstractions;

namespace TmsOmsIntegration.Domain.Orders.Events;

public sealed record OrderEventRejected(
    string OrderNumber,
    OrderStatus CurrentStatus,
    OrderStatus RequestedStatus,
    RejectionReason Reason,
    DateTimeOffset EventDate) : IDomainEvent;
