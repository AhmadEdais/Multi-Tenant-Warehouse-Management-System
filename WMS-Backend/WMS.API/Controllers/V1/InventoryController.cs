namespace WMS.API.Controllers.V1;

[Route("api/v1/[controller]")]
[ApiController]
public class InventoryController(ISender sender) : ControllerBase
{
    [HttpGet("product/{productId}")]
    [Authorize(Policy = SecurityPolicies.CanViewInventory)]
    [ProducesResponseType(typeof(PagedResult<StockLevelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStockByProduct([FromRoute] int productId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var query = new GetStockByProductQuery(productId, pageNumber, pageSize);
        var result = await sender.Send(query);
        return Ok(result);
    }
    [HttpGet("Location/{locationId}")]
    [Authorize(Policy = SecurityPolicies.CanViewInventory)]
    [ProducesResponseType(typeof(PagedResult<StockLevelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStockByLocation([FromRoute] int locationId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var query = new GetStockByLocationQuery(locationId,pageNumber, pageSize);
        var result = await sender.Send(query);
        return Ok(result);
    }
    [HttpGet("summary")]
    [Authorize(Policy = SecurityPolicies.CanViewInventorySummary)]
    [ProducesResponseType(typeof(List<TenantStockSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)] 
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetInventorySummary()
    {
        var query = new GetTenantStockSummaryQuery();
        var result = await sender.Send(query);
        return Ok(result);
    }
}