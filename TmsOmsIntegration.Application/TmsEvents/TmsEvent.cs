using TmsOmsIntegration.Domain.Orders;

namespace TmsOmsIntegration.Application.TmsEvents;

public sealed record TmsEvent(
    ServiceType ServiceType,
    DispatchType? DispatchType,
    OrderStatus Status,
    string? SubStatus,
    string? VehicleCode,
    string? CourierName,
    string OrderNumber,
    string? TrackingNumber,
    string? ClientCode,
    string? ClientName,
    string? ReceivedBy,
    string? Comments,
    IReadOnlyList<TmsEvidence> Evidences,
    DateTimeOffset EventDate);
