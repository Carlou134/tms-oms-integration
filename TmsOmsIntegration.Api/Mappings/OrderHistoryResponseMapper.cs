using TmsOmsIntegration.Api.Responses;
using TmsOmsIntegration.Domain.Orders;

namespace TmsOmsIntegration.Api.Mappings;

internal static class OrderHistoryResponseMapper
{
    public static OrderHistoryEntryResponse ToResponse(this OrderHistoryEntry entry) =>
        new(
            entry.Id,
            entry.OrderNumber,
            entry.Phase,
            entry.Status,
            entry.SubStatus,
            entry.Outcome,
            entry.RejectionReason,
            entry.IsAutomatic,
            entry.VisitCount,
            entry.CourierName,
            entry.VehicleCode,
            entry.EventDate,
            entry.RecordedAt);
}
