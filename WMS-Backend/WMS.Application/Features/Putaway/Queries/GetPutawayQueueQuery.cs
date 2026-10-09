namespace WMS.Application.Features.Putaway.Queries;

public sealed record PutawayQueueItemDto(
    int ProductId,
    string SKU,
    string ProductName,
    decimal QuantityOnHand);

public sealed record GetPutawayQueueQuery(
    int WarehouseId,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<PutawayQueueItemDto>>;

public sealed class GetPutawayQueueQueryValidator : AbstractValidator<GetPutawayQueueQuery>
{
    public GetPutawayQueueQueryValidator()
    {
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

internal sealed class GetPutawayQueueQueryHandler(IWmsDbContext context, ITenantContext tenantContext)
    : IRequestHandler<GetPutawayQueueQuery, PagedResult<PutawayQueueItemDto>>
{
    public async Task<PagedResult<PutawayQueueItemDto>> Handle(
        GetPutawayQueueQuery request,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to view the Putaway queue.");

        var warehouseExists = await context.Warehouses.AsNoTracking()
            .AnyAsync(warehouse => warehouse.Id == request.WarehouseId, cancellationToken);
        if (!warehouseExists)
            throw new NotFoundException($"Warehouse with ID {request.WarehouseId} not found.");

        var docks = await context.Locations.AsNoTracking()
            .Where(location => location.WarehouseId == request.WarehouseId
                && location.LocationType == LocationTypes.Dock)
            .Select(location => new { location.Id, location.IsActive, location.ParentLocationId })
            .Take(2)
            .ToListAsync(cancellationToken);
        if (docks.Count != 1 || !docks[0].IsActive || docks[0].ParentLocationId.HasValue)
            throw new ConflictException("The warehouse must have exactly one active root Receiving Dock.");

        var dockId = docks[0].Id;
        var query =
            from stock in context.StockLevels.AsNoTracking()
            where stock.LocationId == dockId && stock.QuantityOnHand > 0
            join product in context.Products.AsNoTracking()
                on stock.ProductId equals product.Id
            select new
            {
                ProductId = product.Id,
                product.SKU,
                ProductName = product.Name,
                stock.QuantityOnHand
            };

        var search = request.SearchTerm?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(item => item.SKU.Contains(search) || item.ProductName.Contains(search));

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(item => item.ProductName)
            .ThenBy(item => item.ProductId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(item => new PutawayQueueItemDto(
                item.ProductId,
                item.SKU,
                item.ProductName,
                item.QuantityOnHand))
            .ToListAsync(cancellationToken);

        return new PagedResult<PutawayQueueItemDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
