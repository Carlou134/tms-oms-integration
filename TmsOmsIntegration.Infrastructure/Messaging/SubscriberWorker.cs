using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TmsOmsIntegration.Application.Abstractions.Messaging;

namespace TmsOmsIntegration.Infrastructure.Messaging;

/// <summary>
/// Reads one subscriber's channel and invokes its handler in a new DI scope per message,
/// so scoped handlers are not captured by this singleton (captive dependency).
/// </summary>
internal sealed class SubscriberWorker<TMessage, THandler>(
    ChannelSubscription<TMessage, THandler> subscription,
    IServiceScopeFactory scopeFactory,
    ILogger<SubscriberWorker<TMessage, THandler>> logger) : BackgroundService
    where TMessage : class
    where THandler : IEventHandler<TMessage>
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in subscription.Reader.ReadAllAsync(stoppingToken))
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<THandler>();

            try
            {
                await handler.HandleAsync(message, stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Retries and dead-lettering are added in the retry policies branch.
                logger.LogError(exception, "{Handler} failed to handle {MessageType}",
                    typeof(THandler).Name, typeof(TMessage).Name);
            }
        }
    }
}
