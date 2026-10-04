using System.Collections.Concurrent;
using TmsOmsIntegration.Application.Abstractions.Persistence;
using TmsOmsIntegration.Domain.Orders;

namespace TmsOmsIntegration.Infrastructure.Persistence;

internal sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<string, Order> _orders = new(
        OrderSeedData.Orders.Select(order => KeyValuePair.Create(order.OrderNumber, order)),
        StringComparer.OrdinalIgnoreCase);

    public Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default) =>
        Task.FromResult(_orders.GetValueOrDefault(orderNumber.Trim()));

    public Task SaveAsync(Order order, CancellationToken cancellationToken = default)
    {
        _orders[order.OrderNumber] = order;
        return Task.CompletedTask;
    }
}
