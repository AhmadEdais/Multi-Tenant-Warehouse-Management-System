namespace WMS.API.Controllers.V1;

[Authorize]
[Route("api/v1/[controller]")]
[ApiController]
public class WarehousesController(ISender sender) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = SecurityPolicies.CanManageWarehouses)]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateWarehouse([FromBody] CreateWarehouseCommand command)
    {
        var warehouseId = await sender.Send(command);
        return Created("", new { Id = warehouseId });
    }
    [HttpGet]
    [Authorize(Policy = SecurityPolicies.CanViewWarehouses)]
    [ProducesResponseType(typeof(PagedResult<WarehouseListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetWarehouses([FromQuery] ListWarehousesQuery query)
    {
        var warehouses = await sender.Send(query);
        return Ok(warehouses);
    }
    [HttpGet("{id}")]
    [Authorize(Policy = SecurityPolicies.CanViewWarehouses)]
    [ProducesResponseType(typeof(WarehouseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetWarehouseById([FromRoute] int id)
    {

        var warehouse = await sender.Send(new GetWarehouseByIdQuery(id));

        return Ok(warehouse);
    }
    [HttpPut("{id}")]
    [Authorize(Policy = SecurityPolicies.CanManageWarehouses)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateWarehouse([FromRoute] int id, [FromBody] UpdateWarehouseCommand command)
    {
        var commandWithId = command with { Id = id };
        await sender.Send(commandWithId);
        return NoContent();
    }
    [HttpPost("{id}/deactivate")]
    [Authorize(Policy = SecurityPolicies.CanManageWarehouses)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeactivateWarehouse([FromRoute] int id)
    {
        await sender.Send(new DeactivateWarehouseCommand(id));
        return NoContent();
    }
}
