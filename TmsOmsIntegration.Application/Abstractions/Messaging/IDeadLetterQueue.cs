namespace TmsOmsIntegration.Application.Abstractions.Messaging;

public interface IDeadLetterQueue
{
    Task AddAsync(DeadLetterMessage message, CancellationToken cancellationToken = default);
}
