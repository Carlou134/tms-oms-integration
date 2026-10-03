using TmsOmsIntegration.Application.TmsEvents;

namespace TmsOmsIntegration.Api.Webhooks.Tms;

internal static class TmsEventRequestMapper
{
    /// <summary>
    /// Assumes the request already passed model validation.
    /// </summary>
    public static TmsEvent ToTmsEvent(this TmsEventRequest request)
    {
        var details = request.Details!;

        return new TmsEvent(
            ServiceType: TmsCodes.ServiceTypes[request.ServiceType!],
            DispatchType: request.DispatchType is null ? null : TmsCodes.DispatchTypes[request.DispatchType],
            Status: TmsCodes.Statuses[request.Status!],
            SubStatus: request.SubStatus,
            VehicleCode: request.VehicleCode,
            CourierName: request.CourierName,
            OrderNumber: details.OrderNumber!,
            TrackingNumber: details.TrackingNumber,
            ClientCode: details.ClientCode,
            ClientName: details.ClientName,
            ReceivedBy: details.ReceivedBy,
            Comments: details.Comments,
            Evidences: details.Evidences?
                .Select(evidence => new TmsEvidence(
                    evidence.Label,
                    evidence.FileType,
                    evidence.FileName,
                    evidence.Url ?? string.Empty))
                .ToList() ?? [],
            EventDate: request.EventDate!.Value);
    }
}
