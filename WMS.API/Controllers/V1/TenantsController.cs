namespace WMS.API.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class TenantsController(ISender sender) : ControllerBase
{
    [Authorize(Policy =SecurityPolicies.CanManageTenants)]
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantCommand command)
    {

        var tenantId = await sender.Send(command);

        return Created("", new { Id = tenantId });
    }
}