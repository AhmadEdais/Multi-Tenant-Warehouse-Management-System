using Microsoft.AspNetCore.Authorization;

namespace WMS.API.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class TenantsController(ISender sender) : ControllerBase
{
    [Authorize(Roles = "SystemAdmin")]
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