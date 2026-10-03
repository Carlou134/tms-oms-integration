using System.Security.Cryptography;
using System.Text;

namespace TmsOmsIntegration.Application.TmsEvents;

/// <summary>
/// The TMS payload has no event id, so the idempotency key is derived from the fields
/// that identify a single occurrence of an event.
/// </summary>
public static class TmsEventKey
{
    public static string From(TmsEvent tmsEvent)
    {
        var raw = string.Join('|',
            Normalize(tmsEvent.OrderNumber),
            tmsEvent.ServiceType,
            tmsEvent.Status,
            Normalize(tmsEvent.SubStatus),
            tmsEvent.EventDate.ToUniversalTime().ToString("O"));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }

    private static string Normalize(string? value) =>
        value?.Trim().ToUpperInvariant() ?? string.Empty;
}
