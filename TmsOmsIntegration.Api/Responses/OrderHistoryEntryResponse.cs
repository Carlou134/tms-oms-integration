using TmsOmsIntegration.Domain.Orders;
using TmsOmsIntegration.Domain.Orders.Events;

namespace TmsOmsIntegration.Api.Responses;

public sealed record OrderHistoryEntryResponse(
    Guid Id,
    string OrderNumber,
    ServiceType Phase,
    OrderStatus Status,
    string? SubStatus,
    HistoryOutcome Outcome,
    RejectionReason? RejectionReason,
    bool IsAutomatic,
    int? VisitCount,
    string? CourierName,
    string? VehicleCode,
    DateTimeOffset EventDate,
    DateTimeOffset RecordedAt);
