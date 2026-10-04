using Microsoft.AspNetCore.Mvc;
using TmsOmsIntegration.Api.Filters;
using TmsOmsIntegration.Api.Mappings;
using TmsOmsIntegration.Api.Requests;
using TmsOmsIntegration.Application.TmsEvents;

namespace TmsOmsIntegration.Api.Controllers
{
    [Route("api/webhooks/tms/events")]
    [ApiController]
    [RequireTmsApiKey]
    public class TmsWebhookController(
        ReceiveTmsEvent receiveTmsEvent,
        ILogger<TmsWebhookController> logger) : ControllerBase
    {
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
}
