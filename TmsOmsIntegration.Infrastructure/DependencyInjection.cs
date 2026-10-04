using Microsoft.Extensions.DependencyInjection;
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
        services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
        services.AddSingleton<IOrderHistoryRepository, InMemoryOrderHistoryRepository>();
        services.AddSingleton<IEvidenceStorage, InMemoryEvidenceStorage>();
        services.AddSingleton<INotificationFormatter, DefaultNotificationFormatter>();
        services.AddSingleton<INotificationFormatter, TiendasPeruanasNotificationFormatter>();

        services.AddHttpClient<IEvidenceDownloader, HttpEvidenceDownloader>(client =>
            client.Timeout = TimeSpan.FromSeconds(30));

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
