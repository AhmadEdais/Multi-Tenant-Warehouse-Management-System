namespace WMS.Application.Common.Models;

public sealed record AccessTokenResponse(string AccessToken, int ExpiresIn);

public sealed record AuthenticationResult(
    string AccessToken,
    int ExpiresIn,
    [property: JsonIgnore] string RefreshToken,
    [property: JsonIgnore] bool RememberMe,
    [property: JsonIgnore] DateTime AbsoluteExpiresAtUtc)
{
    public AccessTokenResponse ToResponse() => new(AccessToken, ExpiresIn);
}
