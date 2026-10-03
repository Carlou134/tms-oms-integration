namespace TmsOmsIntegration.Application.TmsEvents;

public sealed record TmsEvidence(
    string? Label,
    string? FileType,
    string? FileName,
    string Url);
