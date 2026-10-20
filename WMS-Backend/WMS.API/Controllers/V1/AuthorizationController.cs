namespace WMS.API.Controllers.V1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuthorizationController(ISender sender, IConfiguration configuration) : ControllerBase
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

        [AllowAnonymous]
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
        {
            var refreshToken = Request.Cookies[RefreshCookieManager.CookieName];
            if (string.IsNullOrEmpty(refreshToken))
                throw new UnauthorizedAccessException("Invalid refresh credentials.");

            if (!RefreshRequestOriginGuard.IsTrusted(Request, configuration))
                return Problem(statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden", detail: "Untrusted request origin.");

            try
            {
                var result = await sender.Send(new RefreshTokenCommand(refreshToken), cancellationToken);
                RefreshCookieManager.Set(Response, result);
                return Ok(result.ToResponse());
            }
            catch (UnauthorizedAccessException)
            {
                RefreshCookieManager.Clear(Response);
                throw;
            }
        }
    }
}
