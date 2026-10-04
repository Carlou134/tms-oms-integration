using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TmsOmsIntegration.Application.Abstractions.Messaging;

namespace TmsOmsIntegration.Infrastructure.Messaging;

/// <summary>
/// Publishes pending outbox messages in order. A message is removed only after it is published,
/// so a failure leaves it in place for the next tick (at-least-once delivery).
/// </summary>
internal sealed class OutboxDispatcher(
    InMemoryOutbox outbox,
    IEventPublisher eventPublisher,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromMilliseconds(500);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollingInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            while (outbox.TryPeek(out var entry))
            {
                try
                {
                    await entry.PublishAsync(eventPublisher, stoppingToken);
                    outbox.Remove();
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogWarning(exception, "Could not publish {MessageType} from the outbox, retrying on next tick",
                        entry.MessageType);
                    break;
                }
            }
        }
    }
}
