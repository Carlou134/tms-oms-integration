using TmsOmsIntegration.Application.Abstractions.Messaging;

namespace TmsOmsIntegration.Infrastructure.Messaging;

internal sealed record OutboxEntry(
    string MessageType,
    Func<IEventPublisher, CancellationToken, Task> PublishAsync);
