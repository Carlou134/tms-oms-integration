using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Retry;
using TmsOmsIntegration.Application.Abstractions.Messaging;
using TmsOmsIntegration.Application.Abstractions.Notifications;
using TmsOmsIntegration.Application.Abstractions.Persistence;
using TmsOmsIntegration.Application.Abstractions.Storage;
using TmsOmsIntegration.Infrastructure.Messaging;
using TmsOmsIntegration.Infrastructure.Notifications;
using TmsOmsIntegration.Infrastructure.Persistence;
using TmsOmsIntegration.Infrastructure.Storage;

namespace TmsOmsIntegration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
        services.AddSingleton<IEventPublisher, ChannelEventBus>();
        services.AddSingleton<IDeadLetterQueue, InMemoryDeadLetterQueue>();
        services.AddSingleton<InMemoryOutbox>();
        services.AddSingleton<IOutbox>(provider => provider.GetRequiredService<InMemoryOutbox>());
        services.AddHostedService<OutboxDispatcher>();
        services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
        services.AddSingleton<IOrderHistoryRepository, InMemoryOrderHistoryRepository>();
        services.AddSingleton<IEvidenceStorage, InMemoryEvidenceStorage>();
        services.AddSingleton<INotificationFormatter, DefaultNotificationFormatter>();
        services.AddSingleton<INotificationFormatter, TiendasPeruanasNotificationFormatter>();
        services.AddSingleton<INotificationSender, FakePushNotificationSender>();

        services.AddResiliencePipeline(ResiliencePipelines.Consumer, pipeline =>
            pipeline.AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(exception => exception is not OperationCanceledException),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(1)
            }));

        // HttpClient.Timeout would also cut the retries short, so timeouts live in the pipeline instead.
        services.AddHttpClient<IEvidenceDownloader, HttpEvidenceDownloader>()
            .AddResilienceHandler("evidence-download", pipeline =>
            {
                pipeline.AddTimeout(TimeSpan.FromSeconds(60));

                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    Delay = TimeSpan.FromSeconds(2)
                });

                pipeline.AddTimeout(TimeSpan.FromSeconds(10));
            });

        return services;
    }

    /// <summary>
    /// Subscriptions are wired at startup: each one gets its own channel and background worker.
    /// </summary>
    public static IServiceCollection AddSubscriber<TMessage, THandler>(this IServiceCollection services)
        where TMessage : class
        where THandler : class, IEventHandler<TMessage>
    {
        services.AddScoped<THandler>();
        services.AddSingleton<ChannelSubscription<TMessage, THandler>>();
        services.AddSingleton<IChannelSubscription>(provider =>
            provider.GetRequiredService<ChannelSubscription<TMessage, THandler>>());
        services.AddHostedService<SubscriberWorker<TMessage, THandler>>();

        return services;
    }
}
