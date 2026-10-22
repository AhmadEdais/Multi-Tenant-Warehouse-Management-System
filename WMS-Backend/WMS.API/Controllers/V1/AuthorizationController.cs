using WMS.Application.Features.Demo.Commands;
using WMS.API.Services;

namespace WMS.API.Controllers.V1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuthorizationController(ISender sender, IConfiguration configuration,
        DemoProvisioningLimiter demoLimiter) : ControllerBase
    {
        
        [HttpPost("login")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
        [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            var result = await sender.Send(command);
            RefreshCookieManager.Set(Response, result);
            return Ok(result.ToResponse());
        }

        [AllowAnonymous]
        [HttpPost("demo")]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("demo")]
        [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> StartDemo(CancellationToken cancellationToken)
        {
            // Reject even invalid/expired bearer headers before anonymous provisioning.
            if (User.Identities.Any(identity => identity.IsAuthenticated) || Request.Headers.Authorization.Count > 0)
                return Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden",
                    detail: "Live demos can only be started by guests.");
            if (!RefreshRequestOriginGuard.IsTrusted(Request, configuration))
                return Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden",
                    detail: "Untrusted request origin.");
            if (!demoLimiter.TryAcquire())
            {
                Response.Headers.RetryAfter = "5";
                return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Service Unavailable", detail: "Demo creation is busy. Please try again shortly.");
            }
            try
            {
                var result = await sender.Send(new StartDemoCommand(), cancellationToken);
                RefreshCookieManager.Set(Response, result);
                return Ok(result.ToResponse());
            }
            finally { demoLimiter.Release(); }
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

        [AllowAnonymous]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var refreshToken = Request.Cookies[RefreshCookieManager.CookieName];
            var trustedOrigin = RefreshRequestOriginGuard.IsTrusted(Request, configuration);
            if (!trustedOrigin && (Request.Headers.Origin.Count > 0 ||
                !string.IsNullOrEmpty(refreshToken)))
                return Problem(statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden", detail: "Untrusted request origin.");

            await sender.Send(new LogoutCommand(refreshToken), cancellationToken);
            RefreshCookieManager.Clear(Response);
            return NoContent();
        }
    }
}
