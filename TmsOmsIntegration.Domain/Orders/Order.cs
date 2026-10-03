using TmsOmsIntegration.Domain.Abstractions;
using TmsOmsIntegration.Domain.Orders.Events;

namespace TmsOmsIntegration.Domain.Orders;

/// <summary>
/// Aggregate root for an OMS order. State only changes through its own methods,
/// so the visit counter cannot be altered from outside the domain rules.
/// </summary>
public sealed class Order
{
    private readonly List<IDomainEvent> _domainEvents = [];

    private Order(string orderNumber, string? clientCode)
    {
        OrderNumber = orderNumber;
        ClientCode = clientCode;
    }

    public string OrderNumber { get; }

    public string? ClientCode { get; }

    public OrderStatus? Status { get; private set; }

    public ServiceType? Phase { get; private set; }

    public int VisitCount { get; private set; }

    public DateTimeOffset? LastEventDate { get; private set; }

    public bool IsFinal => Status is OrderStatus.Delivered or OrderStatus.Returned;

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    public static Order Create(string orderNumber, string? clientCode = null)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new ArgumentException("Order number is required.", nameof(orderNumber));

        return new Order(orderNumber.Trim(), clientCode);
    }

    public void Apply(ServiceType phase, OrderStatus status, DateTimeOffset eventDate)
    {
        if (IsFinal)
        {
            _domainEvents.Add(new OrderEventRejected(
                OrderNumber, Status!.Value, status, RejectionReason.FinalState, eventDate));
            return;
        }

        var previousStatus = Status;

        Status = status;
        Phase = phase;
        LastEventDate = eventDate;

        _domainEvents.Add(new OrderStatusChanged(
            OrderNumber, previousStatus, status, phase, VisitCount, eventDate));
    }
}
