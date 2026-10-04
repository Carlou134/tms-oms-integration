using System.Collections.Concurrent;
using TmsOmsIntegration.Application.Abstractions.Messaging;

namespace TmsOmsIntegration.Infrastructure.Messaging;

/// <summary>
/// A production outbox is a table written in the same database transaction as the order,
/// storing the message type and its JSON. In memory, the publish call is captured instead.
/// </summary>
internal sealed class InMemoryOutbox : IOutbox
{
    private readonly ConcurrentQueue<OutboxEntry> _pending = new();

    public Task AddAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        // Capturing the generic call keeps TMessage, so the bus routes by the concrete type.
        _pending.Enqueue(new OutboxEntry(
            typeof(TMessage).Name,
            (publisher, token) => publisher.PublishAsync(message, token)));

        return Task.CompletedTask;
    }

    public bool TryPeek(out OutboxEntry entry) => _pending.TryPeek(out entry!);

    public void Remove() => _pending.TryDequeue(out _);
}
