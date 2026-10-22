using System.Text;
using Microsoft.Data.SqlClient;

namespace WMS.API.Configuration;

internal static class ProductionConfigurationValidator
{
    public static void Validate(IConfiguration configuration)
    {
        var errors = new List<string>();

        var connectionString = configuration.GetConnectionString("WmsConnection");
        if (IsUnset(connectionString))
        {
            errors.Add("ConnectionStrings:WmsConnection is required");
        }
        else
        {
            try
            {
                var sqlOptions = new SqlConnectionStringBuilder(connectionString);
                if (string.IsNullOrWhiteSpace(sqlOptions.DataSource) ||
                    string.IsNullOrWhiteSpace(sqlOptions.InitialCatalog))
                    errors.Add("ConnectionStrings:WmsConnection must specify Server and Database");
            }
            catch (Exception exception) when (exception is ArgumentException or FormatException)
            {
                errors.Add("ConnectionStrings:WmsConnection is not a valid SQL Server connection string");
            }
        }

        var secret = configuration["JwtSettings:Secret"];
        if (IsUnset(secret) || Encoding.UTF8.GetByteCount(secret ?? string.Empty) < 32)
            errors.Add("JwtSettings:Secret must be at least 32 UTF-8 bytes");

        if (IsUnset(configuration["JwtSettings:Issuer"]))
            errors.Add("JwtSettings:Issuer is required");
        if (IsUnset(configuration["JwtSettings:Audience"]))
            errors.Add("JwtSettings:Audience is required");

        var hosts = configuration["AllowedHosts"]?.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (hosts.Length == 0 || hosts.Any(host =>
            IsUnset(host) || host.Contains('*') || Uri.CheckHostName(host) == UriHostNameType.Unknown))
            errors.Add("AllowedHosts must list production hostnames without a wildcard, scheme, or port");

        var origins = configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>() ?? [];
        if (origins.Length == 0 || origins.Any(origin =>
            IsUnset(origin) ||
            !Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || uri.IsLoopback ||
            !string.Equals(origin, uri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase)))
            errors.Add("Frontend:AllowedOrigins must contain HTTPS production origins");

        if (IsUnset(configuration["ReverseProxy:TrustedProxies"]))
            errors.Add("ReverseProxy:TrustedProxies must list the trusted proxy IP addresses");

        if (errors.Count > 0)
            throw new InvalidOperationException("Invalid production configuration: " + string.Join("; ", errors));
    }

    private static bool IsUnset(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Trim().Equals("CHANGE_ME", StringComparison.OrdinalIgnoreCase);
}
