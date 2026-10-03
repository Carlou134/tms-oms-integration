using TmsOmsIntegration.Domain.Orders;

namespace TmsOmsIntegration.Api.Mappings;

/// <summary>
/// Translates the TMS wire values (e.g. "AT PICKUP POINT") into domain enums,
/// keeping the external format out of the domain.
/// </summary>
internal static class TmsCodes
{
    public static readonly IReadOnlyDictionary<string, OrderStatus> Statuses =
        new Dictionary<string, OrderStatus>(StringComparer.OrdinalIgnoreCase)
        {
            ["PLANNING"] = OrderStatus.Planning,
            ["STARTED"] = OrderStatus.Started,
            ["AT PICKUP POINT"] = OrderStatus.AtPickupPoint,
            ["COLLECTED"] = OrderStatus.Collected,
            ["NOT COLLECTED"] = OrderStatus.NotCollected,
            ["DELIVERED"] = OrderStatus.Delivered,
            ["NOT DELIVERED"] = OrderStatus.NotDelivered,
            ["TO BE RETURN"] = OrderStatus.ToBeReturn,
            ["RETURNED"] = OrderStatus.Returned,
            ["NOT RETURNED"] = OrderStatus.NotReturned
        };

    public static readonly IReadOnlyDictionary<string, ServiceType> ServiceTypes =
        new Dictionary<string, ServiceType>(StringComparer.OrdinalIgnoreCase)
        {
            ["PICKUP"] = ServiceType.Pickup,
            ["LAST_MILE"] = ServiceType.LastMile,
            ["RETURN"] = ServiceType.Return
        };

    public static readonly IReadOnlyDictionary<string, DispatchType> DispatchTypes =
        new Dictionary<string, DispatchType>(StringComparer.OrdinalIgnoreCase)
        {
            ["HOME_DELIVERY"] = DispatchType.HomeDelivery,
            ["STORE_WITHDRAWAL"] = DispatchType.StoreWithdrawal,
            ["REVERSE"] = DispatchType.Reverse
        };
}
