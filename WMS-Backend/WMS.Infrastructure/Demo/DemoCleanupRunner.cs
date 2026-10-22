using System.Data;
using Microsoft.Extensions.Logging;

namespace WMS.Infrastructure.Demo;

public sealed record DemoCleanupResult(int Deleted, int Skipped, int Failed);

public sealed class DemoCleanupRunner(WmsDbContext context, ILogger<DemoCleanupRunner> logger)
{
    public async Task<DemoCleanupResult> RunAsync(int batchSize, CancellationToken cancellationToken)
    {
        var deleted = 0;
        var skipped = 0;
        var failed = 0;
        var afterTenantId = 0;
        await context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            while (true)
            {
                await using var command = context.Database.GetDbConnection().CreateCommand();
                command.CommandText = "dbo.CleanupExpiredDemoTenants";
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 120;
                var batchParameter = command.CreateParameter();
                batchParameter.ParameterName = "@BatchSize";
                batchParameter.Value = batchSize;
                command.Parameters.Add(batchParameter);
                var cursorParameter = command.CreateParameter();
                cursorParameter.ParameterName = "@AfterTenantId";
                cursorParameter.Value = afterTenantId;
                command.Parameters.Add(cursorParameter);

                var batchCount = 0;
                await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        batchCount++;
                        var tenantId = reader.GetInt32(0);
                        afterTenantId = tenantId;
                        switch (reader.GetInt32(1))
                        {
                            case 1:
                                deleted++;
                                break;
                            case 0:
                                skipped++;
                                break;
                            default:
                                failed++;
                                logger.LogError("Demo cleanup failed for tenant {TenantId}: SQL {ErrorNumber}, {ErrorMessage}",
                                    tenantId, reader.IsDBNull(2) ? null : reader.GetInt32(2),
                                    reader.IsDBNull(3) ? null : reader.GetString(3));
                                break;
                        }
                    }
                }
                if (batchCount < batchSize) break;
            }
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
        return new DemoCleanupResult(deleted, skipped, failed);
    }
}
