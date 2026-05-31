namespace WMS.API.Controllers.V1;

[Route("api/v1/[controller]")]
[Authorize]
[ApiController]
public class InboundController(ISender sender) : ControllerBase
{
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
    [HttpPost("purchase-orders/{purchaseOrderId}/receive")]
    [Authorize(Policy = SecurityPolicies.CanReceiveStock)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReceivePurchaseOrder(int purchaseOrderId, [FromBody] ReceiveStockCommand command)
    {
        var commandWithId = command with { PurchaseOrderId = purchaseOrderId };
        await sender.Send(commandWithId);
        return NoContent();
    }
}