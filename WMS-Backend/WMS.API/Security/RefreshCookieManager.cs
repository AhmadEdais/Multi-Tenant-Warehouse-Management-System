namespace WMS.API.Security;

internal static class RefreshCookieManager
{
    private const string CookieName = "__Host-wms-refresh";

    public static void Set(HttpResponse response, AuthenticationResult authentication)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        };

        if (authentication.RememberMe)
            options.Expires = new DateTimeOffset(authentication.AbsoluteExpiresAtUtc);

        response.Cookies.Append(CookieName, authentication.RefreshToken, options);
    }
}
