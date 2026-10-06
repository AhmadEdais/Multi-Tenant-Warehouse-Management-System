namespace WMS.API.Controllers.V1;

using WMS.Application.Features.Inbound.Queries;

[Route("api/v1/[controller]")]
[Authorize]
[ApiController]
public class InboundController(ISender sender) : ControllerBase
{
    [HttpGet("purchase-orders/{id:int}")]
    [Authorize(Policy = SecurityPolicies.CanViewPurchaseOrders)]
    [ProducesResponseType(typeof(PurchaseOrderDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPurchaseOrderById(int id)
    {
        return Ok(await sender.Send(new GetPurchaseOrderByIdQuery(id)));
    }

    [HttpPost("purchase-orders")]
    [Authorize(Policy = SecurityPolicies.CanManageInbound)]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePurchaseOrder([FromBody] CreatePurchaseOrderCommand command)
    {
        var result = await sender.Send(command);
        return Ok(result);
    }

    [HttpPut("purchase-orders/{id:int}")]
    [Authorize(Policy = SecurityPolicies.CanManageInbound)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdatePurchaseOrder(int id, [FromBody] UpdatePurchaseOrderCommand command)
    {
        await sender.Send(command with { Id = id });
        return NoContent();
    }
}
