using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WMS.API.HealthChecks;

public sealed class SqlServerReadinessHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("WmsConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Unhealthy();
        }

        var connectionOptions = new SqlConnectionStringBuilder(connectionString)
        {
            ConnectTimeout = 3
        };

        try
        {
            await using var connection = new SqlConnection(connectionOptions.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 3;
            var result = await command.ExecuteScalarAsync(cancellationToken);

            return result is int value && value == 1
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy();
        }
        catch (SqlException)
        {
            return HealthCheckResult.Unhealthy();
        }
    }
}
