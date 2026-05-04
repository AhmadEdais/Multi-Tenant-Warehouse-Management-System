namespace WMS.API.Controllers.V1;

[Route("api/v1/[controller]")]
[ApiController]
public class SuppliersController(ISender sender) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = SecurityPolicies.CanManageSuppliers)]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierCommand command)
    {
        var supplierId = await sender.Send(command);
        return Created("", new { Id = supplierId });
    }
    [HttpPut("{id:int}")]
    [Authorize(Policy = SecurityPolicies.CanManageSuppliers)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateSupplier([FromRoute] int id,[FromBody] UpdateSupplierCommand command)
    {
        var commandWithId = command with { Id = id };
        await sender.Send(commandWithId);
        return NoContent();
    }
    [HttpGet("{id:int}")]
    [Authorize(Policy = SecurityPolicies.CanViewSuppliers)]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSupplierById([FromRoute] int id)
    {
        var supplier = await sender.Send(new GetSupplierByIdQuery(id));
        return Ok(supplier);
    }
    [HttpGet]
    [Authorize(Policy = SecurityPolicies.CanViewSuppliers)]
    [ProducesResponseType(typeof(PagedResult<SupplierListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSuppliers([FromQuery] ListSuppliersQuery query)
    {
        var suppliers = await sender.Send(query);
        return Ok(suppliers);
    }
    [HttpPost("{id:int}/deactivate")]
    [Authorize(Policy = SecurityPolicies.CanManageSuppliers)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeactivateSupplier([FromRoute] int id)
    {
        await sender.Send(new DeactivateSupplierCommand(id));
        return NoContent();
    }
}