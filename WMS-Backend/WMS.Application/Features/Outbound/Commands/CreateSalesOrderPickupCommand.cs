using WMS.Application.Common.Operations;
using WMS.Domain.Enums;

namespace WMS.Application.Features.Outbound.Commands;

public sealed record CreateSalesOrderPickupLineDto(long SalesOrderAllocationId, decimal Quantity);

public sealed record CreateSalesOrderPickupCommand(
    [property: JsonIgnore] int Id,
    string? ReceivedBy,
    string? Notes,
    List<CreateSalesOrderPickupLineDto> Lines) : IRequest<SalesOrderPickupResultDto>;

public sealed record SalesOrderPickupResultDto(
    long PickupId, int SalesOrderId, string SalesOrderStatus,
    decimal TotalPickedQuantity, decimal RemainingQuantity);

public sealed class CreateSalesOrderPickupCommandValidator : AbstractValidator<CreateSalesOrderPickupCommand>
{
    public CreateSalesOrderPickupCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.ReceivedBy).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Lines)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(lines => lines.All(line => line is not null
                && line.SalesOrderAllocationId > 0 && line.Quantity > 0
                && line.Quantity < 10000000000000000m
                && decimal.Round(line.Quantity, 2) == line.Quantity))
            .WithMessage("Each pickup line requires a valid allocation and positive decimal(18,2) quantity.")
            .Must(lines => lines.Select(line => line.SalesOrderAllocationId).Distinct().Count() == lines.Count)
            .WithMessage("An allocation can appear only once in a pickup.");
    }
}

internal sealed class CreateSalesOrderPickupCommandHandler(
    IWmsDbContext context, ITenantContext tenantContext, ICurrentUserService currentUser)
    : IRequestHandler<CreateSalesOrderPickupCommand, SalesOrderPickupResultDto>
{
    public async Task<SalesOrderPickupResultDto> Handle(
        CreateSalesOrderPickupCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedAccessException("A tenant workspace is required to confirm a pickup.");
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("A signed-in user is required to confirm a pickup.");
        var actor = userId.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var order = await context.SalesOrders
            .Include(item => item.Lines)
            .ThenInclude(line => line.Allocations)
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Sales Order was not found.");
        if (order.Status is not (SalesOrderStatus.Allocated or SalesOrderStatus.Picked))
            throw new ConflictException("Only an allocated or partially picked Sales Order can be picked up.");
        if (order.TenantId != tenantId || order.Lines.Count == 0
            || order.Lines.Any(line => line.TenantId != tenantId || line.SalesOrderId != order.Id))
            throw new ConflictException("Sales Order lines are inconsistent.");

        var allocations = order.Lines.SelectMany(line => line.Allocations).ToList();
        if (allocations.Count == 0 || allocations.Any(allocation => allocation.TenantId != tenantId))
            throw new ConflictException("Sales Order allocations are inconsistent.");
        var allocationById = allocations.ToDictionary(allocation => allocation.Id);
        var pickedByAllocation = await LoadPickupHistoryAsync(
            order.Id, tenantId, allocationById.Keys.ToArray(), cancellationToken);
        var reservations = SalesOrderPickupOperations.ValidateReservations(order, request.Lines, allocationById, pickedByAllocation);
        var stockById = await LoadAndValidateStockAsync(
            order, tenantId, request.Lines, reservations, cancellationToken);
        var pickup = SalesOrderPickupOperations.ExecutePickup(context, order, tenantId, actor, request, reservations, stockById);

        await context.SaveChangesAsync(cancellationToken);

        return new SalesOrderPickupResultDto(pickup.Id, order.Id, order.Status.ToString(),
            request.Lines.Sum(item => item.Quantity), order.Lines.Sum(line => line.QuantityAllocated));
    }

    private async Task<Dictionary<long, decimal>> LoadPickupHistoryAsync(
        int orderId, int tenantId, long[] allocationIds, CancellationToken cancellationToken)
    {
        // Read all historic quantities once, including any records whose pickup header is inconsistent.
        var history = await (
            from pickupLine in context.SalesOrderPickupLines.AsNoTracking()
            where allocationIds.Contains(pickupLine.SalesOrderAllocationId)
            join historyPickup in context.SalesOrderPickups.AsNoTracking()
                on pickupLine.SalesOrderPickupId equals historyPickup.Id
            group new { pickupLine, historyPickup } by new
            {
                pickupLine.SalesOrderAllocationId, historyPickup.SalesOrderId, historyPickup.TenantId
            } into groupRows
            select new
            {
                groupRows.Key.SalesOrderAllocationId,
                groupRows.Key.SalesOrderId,
                groupRows.Key.TenantId,
                Quantity = groupRows.Sum(row => row.pickupLine.Quantity)
            }).ToListAsync(cancellationToken);
        if (history.Any(row => row.SalesOrderId != orderId || row.TenantId != tenantId))
            throw new ConflictException("Sales Order pickup history is inconsistent.");
        return history.ToDictionary(row => row.SalesOrderAllocationId, row => row.Quantity);
    }

    private async Task<Dictionary<int, StockLevel>> LoadAndValidateStockAsync(
        SalesOrder order, int tenantId, List<CreateSalesOrderPickupLineDto> requestLines,
        SalesOrderPickupOperations.ReservationData reservations, CancellationToken cancellationToken)
    {
        var allocationById = reservations.AllocationsById;
        var stockIds = requestLines.Select(item => allocationById[item.SalesOrderAllocationId].StockLevelId)
            .Distinct().ToArray();
        var stockRows = await (
            from stock in context.StockLevels
            where stockIds.Contains(stock.Id)
            join location in context.Locations on stock.LocationId equals location.Id
            select new { Stock = stock, Location = location }
        ).ToListAsync(cancellationToken);
        return SalesOrderPickupOperations.ValidateStock(order, tenantId, requestLines, reservations,
            stockRows.Select(row => new SalesOrderPickupOperations.StockAtLocation(row.Stock, row.Location)).ToList());
    }

}
