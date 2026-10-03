namespace TmsOmsIntegration.Domain.Orders;

public enum OrderStatus
{
    Planning = 1,
    Started,
    AtPickupPoint,
    Collected,
    NotCollected,
    Delivered,
    NotDelivered,
    ToBeReturn,
    Returned,
    NotReturned
}
