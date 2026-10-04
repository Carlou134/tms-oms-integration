using Microsoft.AspNetCore.Mvc;
using TmsOmsIntegration.Api.Mappings;
using TmsOmsIntegration.Api.Responses;
using TmsOmsIntegration.Application.History;

namespace TmsOmsIntegration.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(GetOrderHistory getOrderHistory) : ControllerBase
{
    [HttpGet("{orderNumber}/history")]
    [ProducesResponseType<IReadOnlyList<OrderHistoryEntryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory(string orderNumber, CancellationToken cancellationToken)
    {
        var history = await getOrderHistory.HandleAsync(orderNumber, cancellationToken);

        if (history is null)
            return NotFound();

        return Ok(history.Select(entry => entry.ToResponse()));
    }
}
