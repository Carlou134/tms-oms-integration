using TmsOmsIntegration.Application.Abstractions.Notifications;

namespace TmsOmsIntegration.Application.Notifications;

public sealed class NotificationFormatterResolver
{
    private readonly Dictionary<string, INotificationFormatter> _formattersByClient;
    private readonly INotificationFormatter _defaultFormatter;

    public NotificationFormatterResolver(IEnumerable<INotificationFormatter> formatters)
    {
        var all = formatters.ToList();

        _defaultFormatter = all.SingleOrDefault(formatter => formatter.ClientCode is null)
            ?? throw new InvalidOperationException("A default notification formatter (ClientCode = null) is required.");

        // ToDictionary throws on duplicates, so two formatters for the same client fail at startup.
        _formattersByClient = all
            .Where(formatter => formatter.ClientCode is not null)
            .ToDictionary(formatter => formatter.ClientCode!, StringComparer.OrdinalIgnoreCase);
    }

    public INotificationFormatter Resolve(string? clientCode) =>
        clientCode is not null && _formattersByClient.TryGetValue(clientCode.Trim(), out var formatter)
            ? formatter
            : _defaultFormatter;
}
