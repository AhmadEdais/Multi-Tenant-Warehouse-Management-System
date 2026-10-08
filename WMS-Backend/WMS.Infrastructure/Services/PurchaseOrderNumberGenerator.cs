using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage;
using WMS.Application.Common.Exceptions;

namespace WMS.Infrastructure.Services;

internal sealed class PurchaseOrderNumberGenerator(
    IWmsDbContext context,
    ITenantContext tenantContext) : IPurchaseOrderNumberGenerator
{
    public async Task<string> ReserveNextAsync(CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedAccessException("A tenant workspace is required to create a Purchase Order.");
        var transaction = context.Database.CurrentTransaction
            ?? throw new InvalidOperationException("Purchase Order numbering requires a database transaction.");

        // The transaction-owned lock also serializes first-time counter creation for this tenant.
        await using (var command = context.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = "sys.sp_getapplock";
            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = 35;
            command.Transaction = transaction.GetDbTransaction();

            var result = command.CreateParameter();
            result.ParameterName = "@RETURN_VALUE";
            result.DbType = DbType.Int32;
            result.Direction = ParameterDirection.ReturnValue;
            command.Parameters.Add(result);

            AddParameter(command, "@Resource", $"PurchaseOrderSequence:{tenantId}");
            AddParameter(command, "@LockMode", "Exclusive");
            AddParameter(command, "@LockOwner", "Transaction");

            var timeout = command.CreateParameter();
            timeout.ParameterName = "@LockTimeout";
            timeout.DbType = DbType.Int32;
            timeout.Value = 30000;
            command.Parameters.Add(timeout);

            await command.ExecuteNonQueryAsync(cancellationToken);
            if (result.Value is not int lockResult || lockResult < 0)
                throw new ConflictException("Could not reserve a Purchase Order number. Please try again.");
        }

        var sequence = await context.PurchaseOrderSequences
            .SingleOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (sequence is null)
        {
            // Existing manually numbered orders are considered only when the tenant's counter is first created.
            var existingCount = await context.PurchaseOrders
                .LongCountAsync(x => x.TenantId == tenantId, cancellationToken);
            var existingNumbers = await context.PurchaseOrders
                .Where(x => x.TenantId == tenantId && x.OrderNumber.StartsWith("PO-"))
                .Select(x => x.OrderNumber)
                .ToListAsync(cancellationToken);

            var lastNumber = existingCount;
            foreach (var orderNumber in existingNumbers)
            {
                var digits = orderNumber[3..].TrimEnd();
                if (digits.Length >= 6 &&
                    long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                    lastNumber = Math.Max(lastNumber, number);
            }

            sequence = PurchaseOrderSequence.Create(tenantId, lastNumber);
            context.PurchaseOrderSequences.Add(sequence);
        }

        if (sequence.LastNumber == long.MaxValue)
            throw new ConflictException("The Purchase Order number range has been exhausted.");

        var nextNumber = sequence.ReserveNext();
        return "PO-" + nextNumber.ToString("D6", CultureInfo.InvariantCulture);
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, string value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = DbType.String;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
