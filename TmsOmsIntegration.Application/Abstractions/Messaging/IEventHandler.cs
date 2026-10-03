namespace TmsOmsIntegration.Application.Abstractions.Messaging;

public interface IEventHandler<in TMessage>
    where TMessage : class
{
    Task HandleAsync(TMessage message, CancellationToken cancellationToken = default);
}
