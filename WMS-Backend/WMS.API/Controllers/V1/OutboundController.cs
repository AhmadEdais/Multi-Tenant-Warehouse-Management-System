using WMS.Application.Features.Outbound.Commands;
using WMS.Application.Features.Outbound.Queries;

namespace WMS.API.Controllers.V1;

[Route("api/v1/outbound")]
[Authorize]
[ApiController]
public class OutboundController(ISender sender) : ControllerBase
{
    [HttpGet("sales-orders")]
    [Authorize(Policy = SecurityPolicies.CanViewSalesOrders)]
    [ProducesResponseType(typeof(PagedResult<SalesOrderListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListSalesOrders([FromQuery] ListSalesOrdersQuery query)
    {
        return Ok(await sender.Send(query));
    }

    [HttpGet("sales-orders/{id:int}")]
    [Authorize(Policy = SecurityPolicies.CanViewSalesOrders)]
    [ProducesResponseType(typeof(SalesOrderDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSalesOrderById(int id)
    {
        return Ok(await sender.Send(new GetSalesOrderByIdQuery(id)));
    }

    [HttpPost("sales-orders")]
    [Authorize(Policy = SecurityPolicies.CanManageOutbound)]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateSalesOrder([FromBody] CreateSalesOrderCommand command)
    {
        return Ok(await sender.Send(command));
    }

    [HttpPost("sales-orders/{id:int}/approve")]
    [Authorize(Policy = SecurityPolicies.CanManageOutbound)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ApproveSalesOrder(int id)
    {
        await sender.Send(new ApproveSalesOrderCommand(id));
        return NoContent();
    }
}
