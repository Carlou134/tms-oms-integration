using TmsOmsIntegration.Domain.Orders;
using TmsOmsIntegration.Domain.Orders.Events;

namespace TmsOmsIntegration.Tests.Domain.Orders;

public class OrderFinalStateTests
{
    private static readonly DateTimeOffset EventDate = new(2025, 4, 15, 16, 53, 7, TimeSpan.FromHours(-5));

    [Theory]
    [InlineData(ServiceType.LastMile, OrderStatus.Delivered)]
    [InlineData(ServiceType.Return, OrderStatus.Returned)]
    public void Apply_WhenOrderIsFinal_RejectsEventAndKeepsState(ServiceType finalPhase, OrderStatus finalStatus)
    {
        var order = CreateOrderIn(finalPhase, finalStatus);

        order.Apply(ServiceType.LastMile, OrderStatus.Planning, EventDate.AddDays(1));

        Assert.Equal(finalStatus, order.Status);
        Assert.Equal(finalPhase, order.Phase);
        Assert.Equal(EventDate, order.LastEventDate);
    }

    [Theory]
    [InlineData(ServiceType.LastMile, OrderStatus.Delivered)]
    [InlineData(ServiceType.Return, OrderStatus.Returned)]
    public void Apply_WhenOrderIsFinal_RaisesRejectionWithFinalStateReason(ServiceType finalPhase, OrderStatus finalStatus)
    {
        var order = CreateOrderIn(finalPhase, finalStatus);

        order.Apply(ServiceType.LastMile, OrderStatus.Planning, EventDate.AddDays(1));

        var rejected = Assert.IsType<OrderEventRejected>(Assert.Single(order.DomainEvents));
        Assert.Equal(finalStatus, rejected.CurrentStatus);
        Assert.Equal(OrderStatus.Planning, rejected.RequestedStatus);
        Assert.Equal(RejectionReason.FinalState, rejected.Reason);
    }

    [Fact]
    public void Apply_WhenOrderIsDelivered_DoesNotCountAnotherVisit()
    {
        var order = CreateOrderIn(ServiceType.LastMile, OrderStatus.Delivered);

        order.Apply(ServiceType.LastMile, OrderStatus.NotDelivered, EventDate.AddDays(1));

        Assert.Equal(1, order.VisitCount);
    }

    [Theory]
    [InlineData(OrderStatus.Planning)]
    [InlineData(OrderStatus.Started)]
    [InlineData(OrderStatus.NotDelivered)]
    [InlineData(OrderStatus.ToBeReturn)]
    [InlineData(OrderStatus.NotReturned)]
    public void Apply_WhenOrderIsNotFinal_AppliesEvent(OrderStatus currentStatus)
    {
        var order = CreateOrderIn(ServiceType.LastMile, currentStatus);

        order.Apply(ServiceType.LastMile, OrderStatus.Planning, EventDate.AddDays(1));

        Assert.Equal(OrderStatus.Planning, order.Status);
        Assert.IsType<OrderStatusChanged>(Assert.Single(order.DomainEvents));
    }

    [Theory]
    [InlineData(OrderStatus.Delivered, true)]
    [InlineData(OrderStatus.Returned, true)]
    [InlineData(OrderStatus.NotDelivered, false)]
    [InlineData(OrderStatus.NotReturned, false)]
    [InlineData(OrderStatus.ToBeReturn, false)]
    public void IsFinal_OnlyForDeliveredAndReturned(OrderStatus status, bool expected)
    {
        var order = CreateOrderIn(ServiceType.LastMile, status);

        Assert.Equal(expected, order.IsFinal);
    }

    [Fact]
    public void IsFinal_WhenNoEventWasApplied_IsFalse()
    {
        var order = Order.Create("2500000006-01");

        Assert.False(order.IsFinal);
    }

    private static Order CreateOrderIn(ServiceType phase, OrderStatus status)
    {
        var order = Order.Create("2500000006-01");
        order.Apply(phase, status, EventDate);
        order.ClearDomainEvents();
        return order;
    }
}
