using System.Collections.Concurrent;
using TmsOmsIntegration.Application.Abstractions.Messaging;

namespace TmsOmsIntegration.Infrastructure.Messaging;

/// <summary>
/// Keys live for the lifetime of the process and are never expired.
/// A production setup would use a shared store with a TTL (e.g. Redis SET NX EX).
/// </summary>
public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, byte> _keys = new();

    public Task<bool> TryRegisterAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_keys.TryAdd(key, 0));

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _keys.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
