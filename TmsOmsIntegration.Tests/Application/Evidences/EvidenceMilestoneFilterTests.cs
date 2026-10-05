using TmsOmsIntegration.Application.Evidences;
using TmsOmsIntegration.Application.TmsEvents;
using TmsOmsIntegration.Domain.Orders;
using TmsOmsIntegration.Tests.Builders;

namespace TmsOmsIntegration.Tests.Application.Evidences;

public class EvidenceMilestoneFilterTests
{
    private static readonly TmsEvidence Photo =
        new("Paquete", ".jpg", "delivered_201054.jpg", "https://tms.example.com/img/delivered_201054.jpg");

    [Theory]
    [InlineData(ServiceType.Pickup, OrderStatus.Collected)]
    [InlineData(ServiceType.Pickup, OrderStatus.NotCollected)]
    [InlineData(ServiceType.LastMile, OrderStatus.Delivered)]
    [InlineData(ServiceType.LastMile, OrderStatus.NotDelivered)]
    [InlineData(ServiceType.Return, OrderStatus.Returned)]
    [InlineData(ServiceType.Return, OrderStatus.NotReturned)]
    public void ShouldStore_MilestoneWithEvidences_ReturnsTrue(ServiceType phase, OrderStatus status)
    {
        var message = ProcessOnNewOrder(EventWithPhoto(phase, status));

        Assert.True(EvidenceMilestoneFilter.ShouldStore(message));
    }

    [Theory]
    [InlineData(ServiceType.LastMile, OrderStatus.Planning)]
    [InlineData(ServiceType.LastMile, OrderStatus.Started)]
    [InlineData(ServiceType.Pickup, OrderStatus.AtPickupPoint)]
    [InlineData(ServiceType.Return, OrderStatus.ToBeReturn)]
    public void ShouldStore_NotAMilestone_ReturnsFalse(ServiceType phase, OrderStatus status)
    {
        var message = ProcessOnNewOrder(EventWithPhoto(phase, status));

        Assert.False(EvidenceMilestoneFilter.ShouldStore(message));
    }

    [Fact]
    public void ShouldStore_MilestoneWithoutEvidences_ReturnsFalse()
    {
        var withoutEvidences = EventWithPhoto(ServiceType.LastMile, OrderStatus.Delivered) with { Evidences = [] };

        var message = ProcessOnNewOrder(withoutEvidences);

        Assert.False(EvidenceMilestoneFilter.ShouldStore(message));
    }

    [Fact]
    public void ShouldStore_EventRejectedBecauseOrderIsFinal_ReturnsFalse()
    {
        var order = Order.Create("2500000006-01");
        order.Apply(ServiceType.LastMile, OrderStatus.Delivered, DateTimeOffset.UnixEpoch);
        order.ClearDomainEvents();

        var lateDelivery = EventWithPhoto(ServiceType.LastMile, OrderStatus.Delivered);
        var message = Process(order, lateDelivery);

        Assert.False(EvidenceMilestoneFilter.ShouldStore(message));
    }

    [Fact]
    public void ShouldStore_ThirdFailedVisitWithAutomaticReturn_ReturnsTrue()
    {
        var order = Order.Create("2500000007-01");
        order.Apply(ServiceType.LastMile, OrderStatus.NotDelivered, DateTimeOffset.UnixEpoch);
        order.Apply(ServiceType.LastMile, OrderStatus.NotDelivered, DateTimeOffset.UnixEpoch.AddDays(1));
        order.ClearDomainEvents();

        var thirdVisit = EventWithPhoto(ServiceType.LastMile, OrderStatus.NotDelivered);
        var message = Process(order, thirdVisit);

        Assert.True(EvidenceMilestoneFilter.ShouldStore(message));
    }

    private static TmsEvent EventWithPhoto(ServiceType phase, OrderStatus status) =>
        TmsEventBuilder.Delivered() with { ServiceType = phase, Status = status, Evidences = [Photo] };

    private static TmsEventProcessed ProcessOnNewOrder(TmsEvent tmsEvent) =>
        Process(Order.Create(tmsEvent.OrderNumber), tmsEvent);

    // Mirrors ProcessTmsEventHandler: the domain events come from a real Order, not hand-built ones.
    private static TmsEventProcessed Process(Order order, TmsEvent tmsEvent)
    {
        order.Apply(tmsEvent.ServiceType, tmsEvent.Status, tmsEvent.EventDate);
        return new TmsEventProcessed(TmsEventKey.From(tmsEvent), tmsEvent, [.. order.DomainEvents]);
    }
}
