namespace TmsOmsIntegration.Domain.Orders;

/// <summary>
/// Aggregate root for an OMS order. State only changes through its own methods,
/// so the visit counter cannot be altered from outside the domain rules.
/// </summary>
public sealed class Order
{
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

    public static Order Create(string orderNumber, string? clientCode = null)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new ArgumentException("Order number is required.", nameof(orderNumber));

        return new Order(orderNumber.Trim(), clientCode);
    }
}
