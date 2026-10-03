namespace TmsOmsIntegration.Application.TmsEvents;

public sealed record TmsEventReceived(
    string EventKey,
    TmsEvent Event,
    DateTimeOffset ReceivedAt);
