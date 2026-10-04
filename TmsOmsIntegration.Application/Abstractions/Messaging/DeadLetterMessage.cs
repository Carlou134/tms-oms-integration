namespace TmsOmsIntegration.Application.Abstractions.Messaging;

public sealed record DeadLetterMessage(
    Guid Id,
    string MessageType,
    string Handler,
    string Payload,
    string Error,
    DateTimeOffset FailedAt);
