namespace WMS.API.Controllers.V1;

[ApiController]
[Authorize(Roles = Roles.SystemAdmin)]
[Route("api/v1/[controller]")]
public class TenantsController(ISender sender) : ControllerBase
{
    [HttpPost("provision")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateTenant([FromBody] ProvisionTenantCommand command)
    {

        var tenantId = await sender.Send(command);

        return Created("", new { Id = tenantId });
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTenants([FromQuery] ListTenantsQuery query)
    {
        return Ok(await sender.Send(query));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantById([FromRoute] int id)
    {
        return Ok(await sender.Send(new GetTenantByIdQuery(id)));
    }

    [HttpPost("{id:int}/deactivate")]
    [HttpPost("{id:int}/suspend")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateTenant([FromRoute] int id)
    {
        await sender.Send(new DeactivateTenantCommand(id));
        return NoContent();
    }

    [HttpPost("{id:int}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReactivateTenant([FromRoute] int id)
    {
        await sender.Send(new ReactivateTenantCommand(id));
        return NoContent();
    }

}
