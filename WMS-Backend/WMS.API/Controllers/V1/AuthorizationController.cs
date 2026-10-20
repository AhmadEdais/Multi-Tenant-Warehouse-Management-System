namespace WMS.API.Controllers.V1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuthorizationController(ISender sender) : ControllerBase
    {
        
        [HttpPost("login")]
        [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            var result = await sender.Send(command);
            RefreshCookieManager.Set(Response, result);
            return Ok(result.ToResponse());
        }
    }
}
