using Microsoft.Extensions.DependencyInjection;
using TmsOmsIntegration.Application.Abstractions.Messaging;
using TmsOmsIntegration.Infrastructure.Messaging;

namespace TmsOmsIntegration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
        services.AddSingleton<IEventPublisher, ChannelEventBus>();

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
