namespace TmsOmsIntegration.Application.Abstractions.Messaging;

/// <summary>
/// Stores messages to be published later by a background dispatcher, so saving state and
/// publishing its events cannot be split by a failure in between.
/// </summary>
public interface IOutbox
{
    Task AddAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;
}
