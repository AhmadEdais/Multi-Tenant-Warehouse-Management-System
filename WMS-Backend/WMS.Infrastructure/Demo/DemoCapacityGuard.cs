using Microsoft.EntityFrameworkCore.Storage;
using WMS.Application.Common.Exceptions;
using WMS.Application.Features.Demo.Interfaces;

namespace WMS.Infrastructure.Demo;

internal sealed class DemoCapacityGuard(IWmsDbContext context) : IDemoCapacityGuard
{
    public async Task CheckBeforeProvisioningAsync(int maximum, CancellationToken cancellationToken)
    {
        if (await CountAsync(cancellationToken) >= maximum)
            throw new ServiceUnavailableException("The live demo is temporarily unavailable. Please try again later.");
    }

    public async Task CheckBeforeCommitAsync(int maximum, CancellationToken cancellationToken)
    {
        // Only admission/commit is serialized, never the expensive seeding stages.
        // Every demo must pass this gate. Transaction ownership releases the lock on rollback too.
        await using var command = CreateCommand();
        command.CommandText = """
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource = N'WMS:DemoCapacity',
                @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 5000;
            SELECT @result;
            """;
        var result = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (result < 0)
            throw new ConflictException("Demo creation is busy. Please try again.");
        // Includes this transaction's new tenant. Other in-flight inserts are skipped:
        // they cannot commit until they acquire this same lock and recount.
        if (await CountAsync(cancellationToken) > maximum)
            throw new ServiceUnavailableException("The live demo is temporarily unavailable. Please try again later.");
    }

    private async Task<int> CountAsync(CancellationToken cancellationToken)
    {
        await using var command = CreateCommand();
        // Force the covering immutable-membership index: unrelated tenant updates must
        // not cause existing demos to be skipped. IsDemo/Id cannot be changed by the app.
        // READCOMMITTEDLOCK also supports databases using READ_COMMITTED_SNAPSHOT.
        // Page locks may block (or cause a safely rolled-back conflict), never admit over capacity.
        command.CommandText = """
            SELECT COUNT(*) FROM dbo.Tenants WITH
                (INDEX(IX_Tenants_DemoCapacity), READPAST, READCOMMITTEDLOCK)
            WHERE IsDemo = 1;
            """;
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private System.Data.Common.DbCommand CreateCommand()
    {
        var transaction = context.Database.CurrentTransaction
            ?? throw new InvalidOperationException("Demo capacity checks require the provisioning transaction.");
        var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandTimeout = 10;
        return command;
    }
}
