using TmsOmsIntegration.Application.Abstractions.Persistence;
using TmsOmsIntegration.Domain.Orders;

namespace TmsOmsIntegration.Infrastructure.Persistence;

internal sealed class InMemoryOrderHistoryRepository : IOrderHistoryRepository
{
    private readonly List<OrderHistoryEntry> _entries = [];
    private readonly Lock _lock = new();

    public Task AppendAsync(IReadOnlyCollection<OrderHistoryEntry> entries, CancellationToken cancellationToken = default)
    {
        lock (_lock)
            _entries.AddRange(entries);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OrderHistoryEntry>> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            IReadOnlyList<OrderHistoryEntry> history = _entries
                .Where(entry => string.Equals(entry.OrderNumber, orderNumber.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(entry => entry.EventDate)
                .ToList();

            return Task.FromResult(history);
        }
    }
}
