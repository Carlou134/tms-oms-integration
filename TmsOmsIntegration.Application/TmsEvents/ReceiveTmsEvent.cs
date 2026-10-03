using TmsOmsIntegration.Application.Abstractions.Messaging;

namespace TmsOmsIntegration.Application.TmsEvents;

public sealed class ReceiveTmsEvent(
    IIdempotencyStore idempotencyStore,
    IEventPublisher eventPublisher,
    TimeProvider timeProvider)
{
    public async Task<ReceiveTmsEventResult> HandleAsync(
        TmsEvent tmsEvent,
        CancellationToken cancellationToken = default)
    {
        var eventKey = TmsEventKey.From(tmsEvent);

        if (!await idempotencyStore.TryRegisterAsync(eventKey, cancellationToken))
            return ReceiveTmsEventResult.Duplicate;

        var message = new TmsEventReceived(eventKey, tmsEvent, timeProvider.GetUtcNow());

        try
        {
            await eventPublisher.PublishAsync(message, cancellationToken);
        }
        catch
        {
            // Without this, the TMS retry would be discarded as a duplicate and the event lost.
            await idempotencyStore.RemoveAsync(eventKey, CancellationToken.None);
            throw;
        }

        return ReceiveTmsEventResult.Accepted;
    }
}
