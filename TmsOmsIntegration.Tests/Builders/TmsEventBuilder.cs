using TmsOmsIntegration.Application.TmsEvents;
using TmsOmsIntegration.Domain.Orders;

namespace TmsOmsIntegration.Tests.Builders;

/// <summary>
/// A valid TMS event based on the sample payload of the statement; tests override only what they care about with "with".
/// </summary>
internal static class TmsEventBuilder
{
    public static TmsEvent Delivered() => new(
        ServiceType: ServiceType.LastMile,
        DispatchType: DispatchType.HomeDelivery,
        Status: OrderStatus.Delivered,
        SubStatus: "CLIENT RECEIVED",
        VehicleCode: "LIMURB06VAN",
        CourierName: "Conductor 46",
        OrderNumber: "2500000006-01",
        TrackingNumber: "OE2500000006-01",
        ClientCode: "01021755",
        ClientName: "TIENDAS PERUANAS S.A.",
        ReceivedBy: "Pepito Perez",
        Comments: "Entregado en domicilio",
        Evidences: [],
        EventDate: new DateTimeOffset(2025, 4, 15, 16, 53, 7, TimeSpan.FromHours(-5)));
}
