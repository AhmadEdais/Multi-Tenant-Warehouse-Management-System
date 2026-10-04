namespace WMS.Application.Features.Inventory.Queries;

public sealed record StockByLocationDto(
    int ProductId,
    string SKU,
    string ProductName,
    int ReorderPoint,
    decimal QuantityOnHand,
    decimal QuantityAllocated,
    decimal AvailableQuantity,
    bool IsLowStock);

public sealed record GetStockByLocationQuery(
    int LocationId,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PagedResult<StockByLocationDto>>;
public class GetStockByLocationQueryValidator : AbstractValidator<GetStockByLocationQuery>
{
    public GetStockByLocationQueryValidator()
    {
        RuleFor(x => x.LocationId).GreaterThan(0);
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
internal sealed class GetStockByLocationQueryHandler(IWmsDbContext context, ITenantContext tenantContext)
    : IRequestHandler<GetStockByLocationQuery, PagedResult<StockByLocationDto>>
{
    public async Task<PagedResult<StockByLocationDto>> Handle(GetStockByLocationQuery request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to view inventory.");

        var locationExists = await context.Locations
            .AsNoTracking()
            .AnyAsync(location => location.Id == request.LocationId, cancellationToken);
        if (!locationExists)
            throw new NotFoundException($"Location with ID {request.LocationId} not found.");

        var query = from stock in context.StockLevels.AsNoTracking()
                    where stock.LocationId == request.LocationId
                    join product in context.Products.AsNoTracking()
                        on stock.ProductId equals product.Id
                    select new StockByLocationDto(
                        product.Id,
                        product.SKU,
                        product.Name,
                        product.ReorderPoint,
                        stock.QuantityOnHand,
                        stock.QuantityAllocated,
                        stock.QuantityOnHand - stock.QuantityAllocated,
                        stock.QuantityOnHand - stock.QuantityAllocated <= product.ReorderPoint);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(item => item.ProductName)
            .ThenBy(item => item.ProductId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<StockByLocationDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
