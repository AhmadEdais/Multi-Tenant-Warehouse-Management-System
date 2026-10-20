namespace WMS.API.Security;

internal static class RefreshCookieManager
{
    public const string CookieName = "__Host-wms-refresh";

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

    public static void Clear(HttpResponse response) => response.Cookies.Delete(CookieName,
        new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });
}
