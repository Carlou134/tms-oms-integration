using System.ComponentModel.DataAnnotations;

namespace TmsOmsIntegration.Api.Requests;

public sealed record TmsEventDetailsRequest
{
    [Required]
    public string? OrderNumber { get; init; }

    public string? TrackingNumber { get; init; }

    public string? ClientCode { get; init; }

    public string? ClientName { get; init; }

    public string? ReceivedBy { get; init; }

    public string? Comments { get; init; }

    public List<TmsEvidenceRequest>? Evidences { get; init; }
}
