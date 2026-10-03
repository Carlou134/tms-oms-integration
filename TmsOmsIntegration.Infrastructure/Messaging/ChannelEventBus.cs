using Microsoft.Extensions.Logging;
using TmsOmsIntegration.Application.Abstractions.Messaging;

namespace TmsOmsIntegration.Infrastructure.Messaging;

/// <summary>
/// In-process publish-subscribe: every message is fanned out to the channel of each subscriber.
/// A production setup would use a durable broker (e.g. Azure Service Bus topics).
/// </summary>
internal sealed class ChannelEventBus(
    IEnumerable<IChannelSubscription> subscriptions,
    ILogger<ChannelEventBus> logger) : IEventPublisher
{
    private readonly ILookup<Type, IChannelSubscription> _subscriptionsByType =
        subscriptions.ToLookup(subscription => subscription.MessageType);

    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var targets = _subscriptionsByType[typeof(TMessage)];

        if (!targets.Any())
        {
            logger.LogWarning("No subscribers registered for {MessageType}", typeof(TMessage).Name);
            return;
        }

        foreach (var subscription in targets)
            await subscription.WriteAsync(message, cancellationToken);
    }
}
