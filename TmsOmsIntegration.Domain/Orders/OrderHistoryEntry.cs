using TmsOmsIntegration.Domain.Orders.Events;

namespace TmsOmsIntegration.Domain.Orders;

public sealed record OrderHistoryEntry
{
    public Guid Id { get; } = Guid.CreateVersion7();

    public required string OrderNumber { get; init; }

    public required ServiceType Phase { get; init; }

    public required OrderStatus Status { get; init; }

    public string? SubStatus { get; init; }

    public required HistoryOutcome Outcome { get; init; }

    public RejectionReason? RejectionReason { get; init; }

    public bool IsAutomatic { get; init; }

    public int? VisitCount { get; init; }

    public string? CourierName { get; init; }

    public string? VehicleCode { get; init; }

    public required DateTimeOffset EventDate { get; init; }

    public required DateTimeOffset RecordedAt { get; init; }
}
