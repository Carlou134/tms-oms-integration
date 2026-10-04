using System.Globalization;
using System.Net.Mime;
using System.Text.Json;
using TmsOmsIntegration.Application.Abstractions.Notifications;
using TmsOmsIntegration.Application.Notifications;
using TmsOmsIntegration.Domain.Orders;

namespace TmsOmsIntegration.Infrastructure.Notifications;

internal sealed class TiendasPeruanasNotificationFormatter : INotificationFormatter
{
    public string? ClientCode => "01021755";

    public ClientNotification Format(OrderStatusNotification notification)
    {
        var payload = new
        {
            pedido = new
            {
                numero = notification.OrderNumber,
                tracking = notification.TrackingNumber
            },
            estado = ToClientStatus(notification.Status),
            detalle = notification.SubStatus,
            intentos = notification.VisitCount,
            recibidoPor = notification.ReceivedBy,
            devolucionAutomatica = notification.IsAutomatic,
            fecha = notification.EventDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
        };

        return new ClientNotification(
            notification.ClientCode,
            MediaTypeNames.Application.Json,
            JsonSerializer.Serialize(payload));
    }

    private static string ToClientStatus(OrderStatus status) =>
        status switch
        {
            OrderStatus.Planning => "PROGRAMADO",
            OrderStatus.Started => "EN_RUTA",
            OrderStatus.AtPickupPoint => "EN_PUNTO_RECOJO",
            OrderStatus.Collected => "RECOGIDO",
            OrderStatus.NotCollected => "NO_RECOGIDO",
            OrderStatus.Delivered => "ENTREGADO",
            OrderStatus.NotDelivered => "NO_ENTREGADO",
            OrderStatus.ToBeReturn => "POR_DEVOLVER",
            OrderStatus.Returned => "DEVUELTO",
            OrderStatus.NotReturned => "NO_DEVUELTO",
            _ => status.ToString().ToUpperInvariant()
        };
}
