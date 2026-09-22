using WMS.Application.Features.Users.Queries;

namespace WMS.API.Controllers.V1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class UsersController(ISender sender) : ControllerBase
    {
        [Authorize(Roles = Roles.TenantAdmin)]
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command)
        {
            var userId = await sender.Send(command);
            return Created("", new { Id = userId });
        }

        [Authorize(Roles = Roles.TenantAdmin)]
        [HttpPut("{userId:int}/roles")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ReplaceUserRoles([FromRoute] int userId, [FromBody] ReplaceUserRolesDto dto)
        {
            var command = new ReplaceUserRolesCommand(userId, dto.RoleIds);
            await sender.Send(command);
            return NoContent();
        }
        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMe()
        {
            var query = new GetCurrentUserQuery();
            var userProfile = await sender.Send(query);

            return Ok(userProfile);
        }

        [Authorize]
        [HttpPut("me/password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangeOwnPassword([FromBody] ChangeOwnPasswordCommand command)
        {
            await sender.Send(command);
            return NoContent();
        }

        [Authorize(Roles = "SystemAdmin")] 
        [HttpPut("{userId}/tenant")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignUserToTenant(int userId, [FromBody] AssignUserToTenantDto dto)
        {
            var command = new AssignUserToTenantCommand(userId, dto.TenantId);
            await sender.Send(command);
            return NoContent();
        }
        [Authorize(Roles = Roles.TenantAdmin)]
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ListUsers([FromQuery] ListUsersQuery query)
        {
            return Ok(await sender.Send(query));
        }

        [Authorize(Roles = Roles.TenantAdmin)]
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserById([FromRoute] int id)
        {
            return Ok(await sender.Send(new GetUserByIdQuery(id)));
        }

        [Authorize(Roles = Roles.TenantAdmin)]
        [HttpPost("{id:int}/deactivate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeactivateUser([FromRoute] int id)
        {
            await sender.Send(new DeactivateUserCommand(id));
            return NoContent();
        }

        [Authorize(Roles = Roles.TenantAdmin)]
        [HttpPost("{id:int}/reactivate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ReactivateUser([FromRoute] int id)
        {
            await sender.Send(new ReactivateUserCommand(id));
            return NoContent();
        }
    }
}
