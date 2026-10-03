namespace TmsOmsIntegration.Api.Requests;

public sealed record TmsEvidenceRequest
{
    public string? Label { get; init; }

    public string? FileType { get; init; }

    public string? FileName { get; init; }

    public string? Url { get; init; }
}
