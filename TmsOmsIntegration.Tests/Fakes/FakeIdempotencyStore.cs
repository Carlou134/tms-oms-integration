using TmsOmsIntegration.Application.Abstractions.Messaging;

namespace TmsOmsIntegration.Tests.Fakes;

internal sealed class FakeIdempotencyStore : IIdempotencyStore
{
    public HashSet<string> Keys { get; } = [];

    public Task<bool> TryRegisterAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(Keys.Add(key));

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        Keys.Remove(key);
        return Task.CompletedTask;
    }
}
