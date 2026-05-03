using WMS.Application.Features.Locations.Queries;

namespace WMS.API.Controllers.V1;

[Route("api/v1/[controller]")]
[ApiController]
[Authorize]
public class LocationsController(ISender sender) : ControllerBase
{
    private readonly ISender _sender = sender;
    [HttpPost]
    [Authorize(Policy =SecurityPolicies.CanManageLocations)]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateLocation([FromBody] CreateLocationCommand command)
    {
        var locationId = await _sender.Send(command);
        return Created("", new { Id = locationId });
    }
    [HttpGet("warehouse/{warehouseId}/tree")]
    [Authorize(Policy =SecurityPolicies.CanViewLocationsTree)]
    [ProducesResponseType(typeof(List<LocationTreeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLocationTree(int warehouseId)
    {
        var result = await _sender.Send(new GetLocationTreeQuery(warehouseId));
        return Ok(result);
    }
    [HttpPut("Update/{id}")]
    [Authorize(Policy =SecurityPolicies.CanManageLocations)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLocation(int id, [FromBody] UpdateLocationCommand command)
    {
        var commandWithId = command with { Id = id };
        await _sender.Send(commandWithId);
        return NoContent();
    }
    [HttpPost("Deactivate/{id}")]
    [Authorize(Policy =SecurityPolicies.CanDeactivateLocations)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateLocation(int id)
    {
        await _sender.Send(new DeactivateLocationCommand(id));
        return NoContent();
    }

}
