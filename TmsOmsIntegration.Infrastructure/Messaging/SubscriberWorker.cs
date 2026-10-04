using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using TmsOmsIntegration.Application.Abstractions.Messaging;

namespace TmsOmsIntegration.Infrastructure.Messaging;

/// <summary>
/// Reads one subscriber's channel and invokes its handler in a new DI scope per attempt,
/// so scoped handlers are not captured by this singleton (captive dependency) and every retry starts clean.
/// </summary>
internal sealed class SubscriberWorker<TMessage, THandler>(
    ChannelSubscription<TMessage, THandler> subscription,
    IServiceScopeFactory scopeFactory,
    ResiliencePipelineProvider<string> pipelineProvider,
    IDeadLetterQueue deadLetterQueue,
    TimeProvider timeProvider,
    ILogger<SubscriberWorker<TMessage, THandler>> logger) : BackgroundService
    where TMessage : class
    where THandler : IEventHandler<TMessage>
{
    private readonly ResiliencePipeline _retryPipeline = pipelineProvider.GetPipeline(ResiliencePipelines.Consumer);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in subscription.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await _retryPipeline.ExecuteAsync(
                    async cancellationToken => await HandleAsync(message, cancellationToken),
                    stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "{Handler} failed to handle {MessageType} after all retries, sent to dead letter queue",
                    typeof(THandler).Name, typeof(TMessage).Name);

                await DeadLetterAsync(message, exception);
            }
        }
    }

    private async Task DeadLetterAsync(TMessage message, Exception exception)
    {
        try
        {
            await deadLetterQueue.AddAsync(new DeadLetterMessage(
                Guid.CreateVersion7(),
                typeof(TMessage).Name,
                typeof(THandler).Name,
                MessageSerializer.Serialize(message),
                exception.Message,
                timeProvider.GetUtcNow()));
        }
        catch (Exception deadLetterException)
        {
            // The worker must keep consuming even if the dead letter queue itself fails.
            logger.LogCritical(deadLetterException, "Could not dead-letter {MessageType} from {Handler}",
                typeof(TMessage).Name, typeof(THandler).Name);
        }
    }

    private async Task HandleAsync(TMessage message, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<THandler>();

        await handler.HandleAsync(message, cancellationToken);
    }
}
