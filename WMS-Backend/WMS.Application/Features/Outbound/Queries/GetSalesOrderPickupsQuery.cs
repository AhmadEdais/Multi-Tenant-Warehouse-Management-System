namespace WMS.Application.Features.Outbound.Queries;

public sealed record SalesOrderPickupLineDto(
    long Id, long SalesOrderAllocationId, long SalesOrderLineId,
    int ProductId, string ProductSku, string ProductName,
    int StockLevelId, int LocationId, string LocationName, string? LocationBarcode,
    decimal Quantity, long? StockMovementId);

public sealed record SalesOrderPickupDto(
    long Id, int SalesOrderId, DateTime PickedOnUtc,
    string PickedBy, string PickedByFullName, string? ReceivedBy, string? Notes,
    decimal TotalPickedQuantity, List<SalesOrderPickupLineDto> Lines);

public sealed record GetSalesOrderPickupsQuery(int Id) : IRequest<List<SalesOrderPickupDto>>;

public sealed class GetSalesOrderPickupsQueryValidator : AbstractValidator<GetSalesOrderPickupsQuery>
{
    public GetSalesOrderPickupsQueryValidator() => RuleFor(query => query.Id).GreaterThan(0);
}

internal sealed class GetSalesOrderPickupsQueryHandler(IWmsDbContext context, ITenantContext tenantContext)
    : IRequestHandler<GetSalesOrderPickupsQuery, List<SalesOrderPickupDto>>
{
    public async Task<List<SalesOrderPickupDto>> Handle(
        GetSalesOrderPickupsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedAccessException("A tenant workspace is required to view Sales Order pickups.");
        if (!await context.SalesOrders.AsNoTracking().AnyAsync(order => order.Id == request.Id, cancellationToken))
            throw new NotFoundException("Sales Order was not found.");

        var pickups = await context.SalesOrderPickups.AsNoTracking()
            .Where(pickup => pickup.SalesOrderId == request.Id)
            .OrderByDescending(pickup => pickup.PickedOnUtc).ThenByDescending(pickup => pickup.Id)
            .Select(pickup => new
            {
                pickup.Id, pickup.SalesOrderId, pickup.PickedOnUtc,
                pickup.PickedBy, pickup.ReceivedBy, pickup.Notes
            }).ToListAsync(cancellationToken);
        if (pickups.Count == 0) return [];

        var pickupIds = pickups.Select(pickup => pickup.Id).ToArray();
        var lines = await (
            from pickupLine in context.SalesOrderPickupLines.AsNoTracking()
            where pickupIds.Contains(pickupLine.SalesOrderPickupId)
            join allocation in context.SalesOrderAllocations.AsNoTracking()
                on pickupLine.SalesOrderAllocationId equals allocation.Id
            join orderLine in context.SalesOrderLines.AsNoTracking()
                on allocation.SalesOrderLineId equals orderLine.Id
            where orderLine.SalesOrderId == request.Id
            join product in context.Products.AsNoTracking()
                on orderLine.ProductId equals product.Id
            join stock in context.StockLevels.AsNoTracking()
                on allocation.StockLevelId equals stock.Id
            join location in context.Locations.AsNoTracking()
                on stock.LocationId equals location.Id
            join movement in context.StockMovements.AsNoTracking()
                on pickupLine.Id equals movement.SalesOrderPickupLineId into movementGroup
            from movement in movementGroup.DefaultIfEmpty()
            orderby pickupLine.Id
            select new
            {
                pickupLine.SalesOrderPickupId,
                Line = new SalesOrderPickupLineDto(
                    pickupLine.Id, allocation.Id, orderLine.Id, orderLine.ProductId,
                    product.SKU, product.Name, stock.Id, location.Id,
                    location.Name, location.Barcode, pickupLine.Quantity,
                    movement == null ? null : (long?)movement.Id)
            }).ToListAsync(cancellationToken);
        var linesByPickup = lines.GroupBy(row => row.SalesOrderPickupId)
            .ToDictionary(group => group.Key, group => group.Select(row => row.Line).ToList());

        var actorIds = pickups.Select(pickup => pickup.PickedBy)
            .Select(actor => int.TryParse(actor, System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out var id) ? id : 0)
            .Where(id => id > 0).Distinct().ToArray();
        var names = await context.Users.AsNoTracking()
            .Where(user => user.TenantId == tenantId && actorIds.Contains(user.Id))
            .Select(user => new { user.Id, user.FullName })
            .ToDictionaryAsync(user => user.Id, user => user.FullName, cancellationToken);

        return pickups.Select(pickup =>
        {
            var pickupLines = linesByPickup.GetValueOrDefault(pickup.Id) ?? [];
            var name = int.TryParse(pickup.PickedBy, System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out var id)
                && names.TryGetValue(id, out var fullName) && !string.IsNullOrWhiteSpace(fullName)
                ? fullName : pickup.PickedBy;
            return new SalesOrderPickupDto(pickup.Id, pickup.SalesOrderId, pickup.PickedOnUtc,
                pickup.PickedBy, name, pickup.ReceivedBy, pickup.Notes,
                pickupLines.Sum(line => line.Quantity), pickupLines);
        }).ToList();
    }
}
