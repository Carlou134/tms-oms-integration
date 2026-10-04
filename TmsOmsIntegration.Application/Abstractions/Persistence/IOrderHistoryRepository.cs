using TmsOmsIntegration.Domain.Orders;

namespace TmsOmsIntegration.Application.Abstractions.Persistence;

/// <summary>
/// Append-only: entries can be added and read, never updated or deleted.
/// </summary>
public interface IOrderHistoryRepository
{
    Task AppendAsync(IReadOnlyCollection<OrderHistoryEntry> entries, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderHistoryEntry>> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
}
