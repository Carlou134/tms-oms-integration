using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using TmsOmsIntegration.Application.Abstractions.Notifications;
using TmsOmsIntegration.Application.Notifications;
using TmsOmsIntegration.Domain.Orders;

namespace TmsOmsIntegration.Infrastructure.Notifications;

internal sealed class DefaultNotificationFormatter : INotificationFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public string? ClientCode => null;

    public ClientNotification Format(OrderStatusNotification notification)
    {
        var payload = new
        {
            notification.OrderNumber,
            notification.TrackingNumber,
            notification.Status,
            notification.SubStatus,
            Message = BuildMessage(notification),
            notification.EventDate
        };

        return new ClientNotification(
            notification.ClientCode,
            MediaTypeNames.Application.Json,
            JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static string BuildMessage(OrderStatusNotification notification) =>
        notification.Status switch
        {
            OrderStatus.Planning => $"Su pedido {notification.OrderNumber} fue programado en una ruta.",
            OrderStatus.Started => $"El courier inició la ruta de su pedido {notification.OrderNumber}.",
            OrderStatus.AtPickupPoint => $"El courier llegó al punto de recojo de su pedido {notification.OrderNumber}.",
            OrderStatus.Collected => $"Su pedido {notification.OrderNumber} fue recogido.",
            OrderStatus.NotCollected => $"No pudimos recoger su pedido {notification.OrderNumber}.",
            OrderStatus.Delivered => $"Su pedido {notification.OrderNumber} fue entregado.",
            OrderStatus.NotDelivered => $"No pudimos entregar su pedido {notification.OrderNumber} (intento {notification.VisitCount}).",
            OrderStatus.ToBeReturn => $"Su pedido {notification.OrderNumber} será devuelto.",
            OrderStatus.Returned => $"Su pedido {notification.OrderNumber} fue devuelto.",
            OrderStatus.NotReturned => $"No pudimos devolver su pedido {notification.OrderNumber}.",
            _ => $"Su pedido {notification.OrderNumber} cambió de estado."
        };
}
