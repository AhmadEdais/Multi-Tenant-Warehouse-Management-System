using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage;
using WMS.Application.Common.Exceptions;

namespace WMS.Infrastructure.Services;

internal sealed class SalesOrderNumberGenerator(
    IWmsDbContext context,
    ITenantContext tenantContext) : ISalesOrderNumberGenerator
{
    public async Task<string> ReserveNextAsync(CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedAccessException("A tenant workspace is required to create a Sales Order.");
        var transaction = context.Database.CurrentTransaction
            ?? throw new InvalidOperationException("Sales Order numbering requires a database transaction.");

        // Serialize both first-row creation and subsequent reservations for this tenant.
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

            AddParameter(command, "@Resource", $"SalesOrderSequence:{tenantId}");
            AddParameter(command, "@LockMode", "Exclusive");
            AddParameter(command, "@LockOwner", "Transaction");

            var timeout = command.CreateParameter();
            timeout.ParameterName = "@LockTimeout";
            timeout.DbType = DbType.Int32;
            timeout.Value = 30000;
            command.Parameters.Add(timeout);

            await command.ExecuteNonQueryAsync(cancellationToken);
            if (result.Value is not int lockResult || lockResult < 0)
                throw new ConflictException("Could not reserve a Sales Order number. Please try again.");
        }

        var sequence = await context.SalesOrderSequences
            .SingleOrDefaultAsync(item => item.TenantId == tenantId, cancellationToken);
        if (sequence is null)
        {
            // This runs once per tenant and also accommodates orders inserted before the counter exists.
            var existingCount = await context.SalesOrders
                .LongCountAsync(order => order.TenantId == tenantId, cancellationToken);
            var existingNumbers = await context.SalesOrders
                .Where(order => order.TenantId == tenantId && order.OrderNumber.StartsWith("SO-"))
                .Select(order => order.OrderNumber)
                .ToListAsync(cancellationToken);
            var lastNumber = existingCount;
            foreach (var orderNumber in existingNumbers)
            {
                var digits = orderNumber[3..].TrimEnd();
                if (digits.Length >= 6
                    && long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                    lastNumber = Math.Max(lastNumber, number);
            }

            sequence = SalesOrderSequence.Create(tenantId, lastNumber);
            context.SalesOrderSequences.Add(sequence);
        }
        if (sequence.LastNumber == long.MaxValue)
            throw new ConflictException("The Sales Order number range has been exhausted.");

        var next = sequence.ReserveNext();
        return "SO-" + next.ToString("D6", CultureInfo.InvariantCulture);
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
