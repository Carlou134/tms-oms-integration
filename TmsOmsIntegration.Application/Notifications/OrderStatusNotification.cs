using TmsOmsIntegration.Domain.Orders;

namespace TmsOmsIntegration.Application.Notifications;

public sealed record OrderStatusNotification(
    string OrderNumber,
    string? TrackingNumber,
    string ClientCode,
    string? ClientName,
    OrderStatus Status,
    string? SubStatus,
    int VisitCount,
    string? ReceivedBy,
    bool IsAutomatic,
    DateTimeOffset EventDate);
