using TmsOmsIntegration.Application.Abstractions.Messaging;

namespace TmsOmsIntegration.Tests.Fakes;

internal sealed class FakeEventPublisher : IEventPublisher
{
    public List<object> Published { get; } = [];

    /// <summary>
    /// When set, every publish throws this exception instead of recording the message.
    /// </summary>
    public Exception? FailWith { get; set; }

    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (FailWith is not null)
            throw FailWith;

        Published.Add(message);
        return Task.CompletedTask;
    }
}
