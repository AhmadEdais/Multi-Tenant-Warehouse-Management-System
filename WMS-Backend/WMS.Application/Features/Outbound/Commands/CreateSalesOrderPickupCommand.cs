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
        var reservations = ValidateReservations(order, request.Lines, allocationById, pickedByAllocation);
        var stockById = await LoadAndValidateStockAsync(
            order, tenantId, request.Lines, reservations, cancellationToken);
        var pickup = ExecutePickup(order, tenantId, actor, request, reservations, stockById);

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

    private static ReservationData ValidateReservations(
        SalesOrder order, List<CreateSalesOrderPickupLineDto> requestLines,
        Dictionary<long, SalesOrderAllocation> allocationById,
        Dictionary<long, decimal> pickedByAllocation)
    {
        decimal totalPreviouslyPicked = 0;
        decimal totalRemaining = 0;
        foreach (var line in order.Lines)
        {
            if (line.Quantity <= 0 || line.QuantityAllocated < 0 || line.QuantityAllocated > line.Quantity
                || line.Allocations.Count == 0)
                throw new ConflictException("Sales Order line reservations are inconsistent.");

            decimal pickedOnLine = 0;
            decimal reservedOnLine = 0;
            foreach (var allocation in line.Allocations)
            {
                if (allocation.SalesOrderLineId != line.Id || allocation.Quantity <= 0)
                    throw new ConflictException("Sales Order allocations are inconsistent.");
                var picked = pickedByAllocation.GetValueOrDefault(allocation.Id);
                var remaining = allocation.Quantity - picked;
                if (picked < 0 || remaining < 0
                    || allocation.Status is not (SalesOrderAllocationStatus.Active or SalesOrderAllocationStatus.Shipped)
                    || allocation.Status == SalesOrderAllocationStatus.Active && remaining <= 0
                    || allocation.Status == SalesOrderAllocationStatus.Shipped && remaining != 0)
                    throw new ConflictException("Sales Order allocation pickup history is inconsistent.");
                pickedOnLine += picked;
                if (allocation.Status == SalesOrderAllocationStatus.Active)
                    reservedOnLine += remaining;
            }
            if (line.QuantityAllocated != reservedOnLine || line.Quantity != pickedOnLine + reservedOnLine)
                throw new ConflictException("Sales Order remaining reservations are inconsistent.");
            totalPreviouslyPicked += pickedOnLine;
            totalRemaining += reservedOnLine;
        }
        if (order.Status == SalesOrderStatus.Allocated && totalPreviouslyPicked != 0
            || order.Status == SalesOrderStatus.Picked && totalPreviouslyPicked <= 0
            || totalRemaining <= 0)
            throw new ConflictException("Sales Order status does not match its pickup history.");

        var lineByAllocation = order.Lines
            .SelectMany(line => line.Allocations.Select(allocation => new { allocation.Id, Line = line }))
            .ToDictionary(row => row.Id, row => row.Line);
        foreach (var item in requestLines)
        {
            if (!allocationById.TryGetValue(item.SalesOrderAllocationId, out var allocation)
                || allocation.Status != SalesOrderAllocationStatus.Active
                || item.Quantity > allocation.Quantity - pickedByAllocation.GetValueOrDefault(allocation.Id))
                throw new ConflictException("A pickup allocation is unavailable or its remaining quantity is insufficient.");
        }
        foreach (var group in requestLines.GroupBy(item => lineByAllocation[item.SalesOrderAllocationId].Id))
        {
            var line = order.Lines.Single(candidate => candidate.Id == group.Key);
            if (group.Sum(item => item.Quantity) > line.QuantityAllocated)
                throw new ConflictException("Pickup exceeds the line's remaining reservation.");
        }

        return new ReservationData(allocationById, lineByAllocation, pickedByAllocation);
    }

    private async Task<Dictionary<int, StockLevel>> LoadAndValidateStockAsync(
        SalesOrder order, int tenantId, List<CreateSalesOrderPickupLineDto> requestLines,
        ReservationData reservations, CancellationToken cancellationToken)
    {
        var allocationById = reservations.AllocationsById;
        var lineByAllocation = reservations.LinesByAllocation;
        var stockIds = requestLines.Select(item => allocationById[item.SalesOrderAllocationId].StockLevelId)
            .Distinct().ToArray();
        var stockRows = await (
            from stock in context.StockLevels
            where stockIds.Contains(stock.Id)
            join location in context.Locations on stock.LocationId equals location.Id
            select new { Stock = stock, Location = location }
        ).ToListAsync(cancellationToken);
        if (stockRows.Count != stockIds.Length)
            throw new ConflictException("One or more reserved stock records are unavailable.");
        var stockById = stockRows.ToDictionary(row => row.Stock.Id);
        foreach (var item in requestLines)
        {
            var allocation = allocationById[item.SalesOrderAllocationId];
            var row = stockById[allocation.StockLevelId];
            if (row.Stock.TenantId != tenantId || row.Location.TenantId != tenantId
                || row.Stock.ProductId != lineByAllocation[allocation.Id].ProductId
                || row.Location.WarehouseId != order.WarehouseId
                || row.Location.LocationType != LocationTypes.Bin)
                throw new ConflictException("Reserved stock does not belong to this product and warehouse Bin.");
        }
        foreach (var group in requestLines.GroupBy(item => allocationById[item.SalesOrderAllocationId].StockLevelId))
        {
            var stock = stockById[group.Key].Stock;
            var quantity = group.Sum(item => item.Quantity);
            if (stock.QuantityAllocated < quantity || stock.QuantityOnHand < quantity)
                throw new ConflictException("Reserved physical stock is insufficient for this pickup.");
        }
        return stockById.ToDictionary(row => row.Key, row => row.Value.Stock);
    }

    private SalesOrderPickup ExecutePickup(
        SalesOrder order, int tenantId, string actor, CreateSalesOrderPickupCommand request,
        ReservationData reservations, Dictionary<int, StockLevel> stockById)
    {
        var now = DateTime.UtcNow;
        var pickup = SalesOrderPickup.Create(tenantId, order.Id, actor, now, request.ReceivedBy, request.Notes);
        context.SalesOrderPickups.Add(pickup);
        foreach (var item in request.Lines)
        {
            var allocation = reservations.AllocationsById[item.SalesOrderAllocationId];
            var line = reservations.LinesByAllocation[allocation.Id];
            var stock = stockById[allocation.StockLevelId];
            var pickupLine = pickup.AddLine(allocation.Id, item.Quantity);
            stock.Ship(item.Quantity, actor, now);
            line.RecordPickup(allocation, item.Quantity);
            if (reservations.PickedByAllocation.GetValueOrDefault(allocation.Id) + item.Quantity == allocation.Quantity)
                allocation.MarkShipped();
            context.StockMovements.Add(StockMovement.CreatePickupShipment(
                tenantId, line.ProductId, stock.LocationId, item.Quantity,
                order.Id, pickupLine, actor, now));
        }
        order.UpdateFulfillmentStatus(actor, now);
        return pickup;
    }

    private sealed record ReservationData(
        Dictionary<long, SalesOrderAllocation> AllocationsById,
        Dictionary<long, SalesOrderLine> LinesByAllocation,
        Dictionary<long, decimal> PickedByAllocation);
}
