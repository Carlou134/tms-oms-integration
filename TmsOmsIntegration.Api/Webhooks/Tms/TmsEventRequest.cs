using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TmsOmsIntegration.Api.Webhooks.Tms;

public sealed record TmsEventRequest : IValidatableObject
{
    [Required]
    public string? ServiceType { get; init; }

    public string? DispatchType { get; init; }

    [Required]
    public string? Status { get; init; }

    public string? SubStatus { get; init; }

    public string? VehicleCode { get; init; }

    public string? CourierName { get; init; }

    [Required]
    public TmsEventDetailsRequest? Details { get; init; }

    [Required]
    [JsonConverter(typeof(TmsEventDateConverter))]
    public DateTimeOffset? EventDate { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Status is not null && !TmsCodes.Statuses.ContainsKey(Status))
            yield return new ValidationResult($"Unknown status '{Status}'.", [nameof(Status)]);

        if (ServiceType is not null && !TmsCodes.ServiceTypes.ContainsKey(ServiceType))
            yield return new ValidationResult($"Unknown serviceType '{ServiceType}'.", [nameof(ServiceType)]);

        if (DispatchType is not null && !TmsCodes.DispatchTypes.ContainsKey(DispatchType))
            yield return new ValidationResult($"Unknown dispatchType '{DispatchType}'.", [nameof(DispatchType)]);
    }
}
