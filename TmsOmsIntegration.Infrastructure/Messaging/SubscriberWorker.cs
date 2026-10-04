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
                // Dead-lettering is added in the next commit.
                logger.LogError(exception, "{Handler} failed to handle {MessageType} after all retries",
                    typeof(THandler).Name, typeof(TMessage).Name);
            }
        }
    }

    private async Task HandleAsync(TMessage message, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<THandler>();

        await handler.HandleAsync(message, cancellationToken);
    }
}
