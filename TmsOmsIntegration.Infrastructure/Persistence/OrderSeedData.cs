using TmsOmsIntegration.Domain.Orders;

namespace TmsOmsIntegration.Infrastructure.Persistence;

/// <summary>
/// Stands in for the OMS: the integration only updates orders that already exist there.
/// </summary>
internal static class OrderSeedData
{
    public static IEnumerable<Order> Orders =>
    [
        Order.Create("2500000006-01", "01021755"),
        Order.Create("2500000007-01", "01021755"),
        Order.Create("2500000008-01", "01021800"),
        Order.Create("2500000009-01", "01021900")
    ];
}
