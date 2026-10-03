namespace TmsOmsIntegration.Application.Abstractions.Messaging;

public interface IIdempotencyStore
{
    /// <summary>
    /// Atomically registers the key. Returns false if it was already registered.
    /// </summary>
    Task<bool> TryRegisterAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a key so the sender can retry, used when processing fails after registering it.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
