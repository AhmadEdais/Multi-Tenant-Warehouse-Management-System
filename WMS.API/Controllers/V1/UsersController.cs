using WMS.Application.Features.Users.Queries;

namespace WMS.API.Controllers.V1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;
        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [Authorize(Roles = "SystemAdmin, TenantAdmin")] 
        [HttpPost("{userId}/roles")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AssignRole(int userId, [FromBody] AssignRoleDto dto)
        {

            var command = new AssignRoleCommand(userId, dto.RoleId);
            await _mediator.Send(command);
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
            var userProfile = await _mediator.Send(query);

            return Ok(userProfile);
        }
    }
}
