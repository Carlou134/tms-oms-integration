using TmsOmsIntegration.Domain.Orders;
using TmsOmsIntegration.Domain.Orders.Events;

namespace TmsOmsIntegration.Tests.Domain.Orders;

public class OrderVisitTests
{
    private static readonly DateTimeOffset EventDate = new(2025, 4, 15, 10, 0, 0, TimeSpan.FromHours(-5));

    [Theory]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.NotDelivered)]
    public void Apply_DeliveryAttempt_CountsOneVisit(OrderStatus status)
    {
        var order = Order.Create("2500000007-01");

        order.Apply(ServiceType.LastMile, status, EventDate);

        Assert.Equal(1, order.VisitCount);
    }

    [Theory]
    [InlineData(ServiceType.Pickup, OrderStatus.Planning)]
    [InlineData(ServiceType.Pickup, OrderStatus.Started)]
    [InlineData(ServiceType.Pickup, OrderStatus.AtPickupPoint)]
    [InlineData(ServiceType.Pickup, OrderStatus.Collected)]
    [InlineData(ServiceType.Pickup, OrderStatus.NotCollected)]
    [InlineData(ServiceType.Return, OrderStatus.ToBeReturn)]
    [InlineData(ServiceType.Return, OrderStatus.Returned)]
    [InlineData(ServiceType.Return, OrderStatus.NotReturned)]
    public void Apply_OtherStatus_DoesNotCountVisit(ServiceType phase, OrderStatus status)
    {
        var order = Order.Create("2500000007-01");

        order.Apply(phase, status, EventDate);

        Assert.Equal(0, order.VisitCount);
    }

    [Fact]
    public void Apply_TwoFailedVisits_DoesNotTriggerReturn()
    {
        var order = Order.Create("2500000007-01");

        ApplyFailedVisits(order, count: 2);

        Assert.Equal(2, order.VisitCount);
        Assert.Equal(OrderStatus.NotDelivered, order.Status);
        Assert.DoesNotContain(order.DomainEvents.OfType<OrderStatusChanged>(), changed => changed.IsAutomatic);
    }

    [Fact]
    public void Apply_ThirdFailedVisit_MovesOrderToReturn()
    {
        var order = Order.Create("2500000007-01");

        ApplyFailedVisits(order, count: 3);

        Assert.Equal(3, order.VisitCount);
        Assert.Equal(OrderStatus.ToBeReturn, order.Status);
        Assert.Equal(ServiceType.Return, order.Phase);
    }

    [Fact]
    public void Apply_ThirdFailedVisit_RaisesFailedVisitAndAutomaticReturnInOrder()
    {
        var order = Order.Create("2500000007-01");
        ApplyFailedVisits(order, count: 2);
        order.ClearDomainEvents();

        var thirdVisitDate = EventDate.AddDays(10);
        order.Apply(ServiceType.LastMile, OrderStatus.NotDelivered, thirdVisitDate);

        var changes = order.DomainEvents.OfType<OrderStatusChanged>().ToList();
        Assert.Equal(2, changes.Count);

        Assert.Equal(OrderStatus.NotDelivered, changes[0].NewStatus);
        Assert.False(changes[0].IsAutomatic);

        Assert.Equal(OrderStatus.ToBeReturn, changes[1].NewStatus);
        Assert.Equal(OrderStatus.NotDelivered, changes[1].PreviousStatus);
        Assert.True(changes[1].IsAutomatic);
        Assert.Equal(3, changes[1].VisitCount);
        Assert.Equal(thirdVisitDate, changes[1].EventDate);
    }

    [Fact]
    public void Apply_ThirdVisitDelivered_DoesNotTriggerReturn()
    {
        var order = Order.Create("2500000007-01");
        ApplyFailedVisits(order, count: 2);
        order.ClearDomainEvents();

        order.Apply(ServiceType.LastMile, OrderStatus.Delivered, EventDate.AddDays(10));

        Assert.Equal(3, order.VisitCount);
        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.IsType<OrderStatusChanged>(Assert.Single(order.DomainEvents));
    }

    [Fact]
    public void Apply_FourthFailedVisit_DoesNotTriggerAnotherReturn()
    {
        var order = Order.Create("2500000007-01");
        ApplyFailedVisits(order, count: 3);
        order.ClearDomainEvents();

        order.Apply(ServiceType.LastMile, OrderStatus.NotDelivered, EventDate.AddDays(10));

        Assert.Equal(4, order.VisitCount);
        Assert.Equal(OrderStatus.NotDelivered, order.Status);
        Assert.IsType<OrderStatusChanged>(Assert.Single(order.DomainEvents));
    }

    // Simulates a realistic route: every failed attempt is preceded by a new PLANNING.
    private static void ApplyFailedVisits(Order order, int count)
    {
        for (var visit = 0; visit < count; visit++)
        {
            order.Apply(ServiceType.LastMile, OrderStatus.Planning, EventDate.AddDays(visit));
            order.Apply(ServiceType.LastMile, OrderStatus.NotDelivered, EventDate.AddDays(visit).AddHours(4));
        }
    }
}
