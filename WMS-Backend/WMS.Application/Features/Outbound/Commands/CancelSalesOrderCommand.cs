using WMS.Domain.Enums;

namespace WMS.Application.Features.Outbound.Commands;

public sealed record CancelSalesOrderCommand(int Id) : IRequest;

public sealed class CancelSalesOrderCommandValidator : AbstractValidator<CancelSalesOrderCommand>
{
    public CancelSalesOrderCommandValidator() => RuleFor(command => command.Id).GreaterThan(0);
}

internal sealed class CancelSalesOrderCommandHandler(
    IWmsDbContext context,
    ITenantContext tenantContext,
    ICurrentUserService currentUser) : IRequestHandler<CancelSalesOrderCommand>
{
    public async Task Handle(CancelSalesOrderCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedAccessException("A tenant workspace is required to cancel a Sales Order.");
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("A signed-in user is required to cancel a Sales Order.");
        var actor = userId.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var order = await context.SalesOrders
            .Include(item => item.Lines)
            .ThenInclude(line => line.Allocations)
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Sales Order was not found.");

        if (order.Status is not (SalesOrderStatus.Draft or SalesOrderStatus.Allocated))
            throw new ConflictException("Only a draft or allocated Sales Order can be canceled.");

        if (order.Status == SalesOrderStatus.Draft)
        {
            if (order.Lines.Any(line => line.QuantityAllocated != 0 || line.Allocations.Count != 0))
                throw new ConflictException("A draft Sales Order has unexpected reservations and cannot be canceled.");

            order.Cancel(actor, DateTime.UtcNow);
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        if (order.Lines.Count == 0)
            throw new ConflictException("An allocated Sales Order has no lines to release.");

        foreach (var line in order.Lines)
        {
            if (line.QuantityAllocated != line.Quantity || line.Allocations.Count == 0
                || line.Allocations.Any(allocation => allocation.Status != SalesOrderAllocationStatus.Active
                    || allocation.SalesOrderLineId != line.Id || allocation.TenantId != tenantId
                    || allocation.Quantity <= 0)
                || line.Allocations.Sum(allocation => allocation.Quantity) != line.QuantityAllocated)
                throw new ConflictException("Sales Order reservation quantities are inconsistent.");
        }

        var allocations = order.Lines.SelectMany(line => line.Allocations).ToList();
        var stockIds = allocations.Select(allocation => allocation.StockLevelId).Distinct().ToArray();
        var stockRows = await (
            from stock in context.StockLevels
            where stockIds.Contains(stock.Id)
            join location in context.Locations on stock.LocationId equals location.Id
            select new { Stock = stock, location.WarehouseId, location.LocationType }
        ).ToListAsync(cancellationToken);

        if (stockRows.Count != stockIds.Length)
            throw new ConflictException("One or more allocated stock records are no longer available.");

        var stockById = stockRows.ToDictionary(row => row.Stock.Id);
        foreach (var line in order.Lines)
        {
            foreach (var allocation in line.Allocations)
            {
                var row = stockById[allocation.StockLevelId];
                if (row.Stock.TenantId != tenantId || row.Stock.ProductId != line.ProductId
                    || row.WarehouseId != order.WarehouseId || row.LocationType != LocationTypes.Bin)
                    throw new ConflictException("A Sales Order allocation does not belong to its product and warehouse Bin.");
            }
        }

        foreach (var group in allocations.GroupBy(allocation => allocation.StockLevelId))
        {
            if (stockById[group.Key].Stock.QuantityAllocated < group.Sum(allocation => allocation.Quantity))
                throw new ConflictException("Allocated stock is insufficient to release this Sales Order.");
        }

        var now = DateTime.UtcNow;
        foreach (var line in order.Lines)
        {
            foreach (var allocation in line.Allocations)
            {
                stockById[allocation.StockLevelId].Stock.Release(allocation.Quantity, actor, now);
                line.ReleaseAllocation(allocation, actor, now, "Sales Order canceled");
            }
        }

        order.Cancel(actor, now);
        await context.SaveChangesAsync(cancellationToken);
    }
}
