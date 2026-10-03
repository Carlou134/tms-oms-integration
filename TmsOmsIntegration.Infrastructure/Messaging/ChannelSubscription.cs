using System.Threading.Channels;
using TmsOmsIntegration.Application.Abstractions.Messaging;

namespace TmsOmsIntegration.Infrastructure.Messaging;

internal interface IChannelSubscription
{
    Type MessageType { get; }

    ValueTask WriteAsync(object message, CancellationToken cancellationToken);
}

/// <summary>
/// One channel per subscriber: a Channel has a single logical reader, so sharing one
/// across subscribers would deliver each message to only one of them (competing consumers).
/// </summary>
internal sealed class ChannelSubscription<TMessage, THandler> : IChannelSubscription
    where TMessage : class
    where THandler : IEventHandler<TMessage>
{
    private const int Capacity = 1_000;

    private readonly Channel<TMessage> _channel = Channel.CreateBounded<TMessage>(
        new BoundedChannelOptions(Capacity)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait
        });

    public Type MessageType => typeof(TMessage);

    public ChannelReader<TMessage> Reader => _channel.Reader;

    public ValueTask WriteAsync(object message, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync((TMessage)message, cancellationToken);
}
