using WMS.Application.Features.Putaway.Commands;
using WMS.Application.Features.Putaway.Queries;

namespace WMS.API.Controllers.V1;

[Route("api/v1/putaway")]
[Authorize]
[ApiController]
public class PutawayController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = SecurityPolicies.CanPutaway)]
    [ProducesResponseType(typeof(PagedResult<PutawayQueueItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetQueue([FromQuery] GetPutawayQueueQuery query)
    {
        return Ok(await sender.Send(query));
    }

    [HttpPost]
    [Authorize(Policy = SecurityPolicies.CanPutaway)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Putaway([FromBody] PutawayCommand command)
    {
        await sender.Send(command);
        return NoContent();
    }
}
