namespace WMS.API.Controllers.V1;

[ApiController]
[Authorize(Roles = "SystemAdmin")]
[Route("api/v1/[controller]")]
public class TenantsController(ISender sender) : ControllerBase
{
    [HttpPost("provision")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateTenant([FromBody] ProvisionTenantCommand command)
    {

        var tenantId = await sender.Send(command);

        return Created("", new { Id = tenantId });
    }
   

}