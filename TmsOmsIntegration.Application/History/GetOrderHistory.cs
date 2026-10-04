using TmsOmsIntegration.Application.Abstractions.Persistence;
using TmsOmsIntegration.Domain.Orders;

namespace TmsOmsIntegration.Application.History;

public sealed class GetOrderHistory(
    IOrderRepository orderRepository,
    IOrderHistoryRepository historyRepository)
{
    /// <summary>
    /// Returns null when the order does not exist, and an empty list when it has no events yet.
    /// </summary>
    public async Task<IReadOnlyList<OrderHistoryEntry>?> HandleAsync(
        string orderNumber,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByOrderNumberAsync(orderNumber, cancellationToken);

        if (order is null)
            return null;

        return await historyRepository.GetByOrderNumberAsync(order.OrderNumber, cancellationToken);
    }
}
