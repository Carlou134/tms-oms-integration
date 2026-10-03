using TmsOmsIntegration.Domain.Abstractions;

namespace TmsOmsIntegration.Application.TmsEvents;

public sealed record TmsEventProcessed(
    string EventKey,
    TmsEvent Event,
    IReadOnlyList<IDomainEvent> DomainEvents);
