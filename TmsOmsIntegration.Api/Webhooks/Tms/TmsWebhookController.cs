using Microsoft.AspNetCore.Mvc;
using TmsOmsIntegration.Application.TmsEvents;

namespace TmsOmsIntegration.Api.Webhooks.Tms;

[ApiController]
[Route("api/webhooks/tms/events")]
[RequireTmsApiKey]
public sealed class TmsWebhookController(
    ReceiveTmsEvent receiveTmsEvent,
    ILogger<TmsWebhookController> logger) : ControllerBase
{
    /// <summary>
    /// Only validates and enqueues; business rules run in the subscribers.
    /// Duplicates also return 202 so the TMS stops resending them.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Receive(TmsEventRequest request, CancellationToken cancellationToken)
    {
        var tmsEvent = request.ToTmsEvent();
        var result = await receiveTmsEvent.HandleAsync(tmsEvent, cancellationToken);

        if (result == ReceiveTmsEventResult.Duplicate)
            logger.LogInformation("Duplicate {Status} event ignored for order {OrderNumber}",
                tmsEvent.Status, tmsEvent.OrderNumber);

        return Accepted();
    }
}
