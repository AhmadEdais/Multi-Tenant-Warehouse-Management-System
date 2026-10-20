namespace WMS.API.Security;

internal static class RefreshRequestOriginGuard
{
    public static bool IsTrusted(HttpRequest request, IConfiguration configuration)
    {
        var origin = request.Headers.Origin.ToString();
        if (string.IsNullOrEmpty(origin))
            return request.Headers["Sec-Fetch-Site"].ToString() == "same-origin";

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri) ||
            originUri.Scheme is not ("http" or "https") ||
            originUri.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(originUri.Query) ||
            !string.IsNullOrEmpty(originUri.Fragment) ||
            !string.IsNullOrEmpty(originUri.UserInfo))
            return false;

        var requestOrigin = $"{request.Scheme}://{request.Host}";
        var normalizedOrigin = originUri.GetLeftPart(UriPartial.Authority);
        if (string.Equals(normalizedOrigin, requestOrigin, StringComparison.OrdinalIgnoreCase))
            return true;

        var trustedFrontendOrigins = configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>() ?? [];
        return trustedFrontendOrigins.Any(trusted =>
            string.Equals(trusted.TrimEnd('/'), normalizedOrigin, StringComparison.OrdinalIgnoreCase));
    }
}
