using System.Collections.Concurrent;
using TmsOmsIntegration.Application.Abstractions.Messaging;

namespace TmsOmsIntegration.Infrastructure.Messaging;

/// <summary>
/// A production setup would use the broker's native dead-letter queue (e.g. Azure Service Bus).
/// </summary>
internal sealed class InMemoryDeadLetterQueue : IDeadLetterQueue
{
    private readonly ConcurrentQueue<DeadLetterMessage> _messages = new();

    public Task AddAsync(DeadLetterMessage message, CancellationToken cancellationToken = default)
    {
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }
}
