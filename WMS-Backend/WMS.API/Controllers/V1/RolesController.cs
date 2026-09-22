using WMS.Application.Features.RoleCatalog.Queries;

namespace WMS.API.Controllers.V1;

[ApiController]
[Authorize(Roles = Roles.TenantAdmin)]
[Route("api/v1/[controller]")]
public class RolesController(ISender sender) : ControllerBase
{
    [HttpGet("assignable")]
    [ProducesResponseType(typeof(List<AssignableRoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAssignableRoles()
    {
        return Ok(await sender.Send(new GetAssignableRolesQuery()));
    }
}
