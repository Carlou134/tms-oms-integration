namespace TmsOmsIntegration.Application.Abstractions.Messaging;

public interface IIdempotencyStore
{
    /// <summary>
    /// Atomically registers the key. Returns false if it was already registered.
    /// </summary>
    Task<bool> TryRegisterAsync(string key, CancellationToken cancellationToken = default);
}
