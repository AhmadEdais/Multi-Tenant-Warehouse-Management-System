namespace WMS.Application.Features.Inventory.Queries;

public sealed record StockByProductDto(
    int LocationId,
    string LocationName,
    string LocationType,
    int WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    decimal QuantityOnHand,
    decimal QuantityAllocated,
    decimal AvailableQuantity);
public record GetStockByProductQuery(
    int ProductId,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PagedResult<StockByProductDto>>;
public class GetStockByProductQueryValidator : AbstractValidator<GetStockByProductQuery>
{
    public GetStockByProductQueryValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("ProductId must be greater than 0.");
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
internal sealed class GetStockByProductQueryHandler(IWmsDbContext context, ITenantContext tenantContext)
    : IRequestHandler<GetStockByProductQuery, PagedResult<StockByProductDto>>
{
    public async Task<PagedResult<StockByProductDto>> Handle(
        GetStockByProductQuery request,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to view inventory.");

        var productExists = await context.Products
            .AsNoTracking()
            .AnyAsync(product => product.Id == request.ProductId, cancellationToken);
        if (!productExists)
            throw new NotFoundException($"Product with ID {request.ProductId} not found.");

        var query = from stock in context.StockLevels.AsNoTracking()
                    where stock.ProductId == request.ProductId
                    join location in context.Locations.AsNoTracking()
                        on stock.LocationId equals location.Id
                    join warehouse in context.Warehouses.AsNoTracking()
                        on location.WarehouseId equals warehouse.Id
                    select new StockByProductDto(
                        location.Id,
                        location.Name,
                        location.LocationType,
                        warehouse.Id,
                        warehouse.Code,
                        warehouse.Name,
                        stock.QuantityOnHand,
                        stock.QuantityAllocated,
                        stock.QuantityOnHand - stock.QuantityAllocated);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(item => item.WarehouseName)
            .ThenBy(item => item.LocationName)
            .ThenBy(item => item.LocationId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<StockByProductDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
