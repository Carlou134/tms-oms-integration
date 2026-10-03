using TmsOmsIntegration.Domain.Orders;

namespace TmsOmsIntegration.Application.Abstractions.Persistence;

public interface IOrderRepository
{
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

    Task SaveAsync(Order order, CancellationToken cancellationToken = default);
}
