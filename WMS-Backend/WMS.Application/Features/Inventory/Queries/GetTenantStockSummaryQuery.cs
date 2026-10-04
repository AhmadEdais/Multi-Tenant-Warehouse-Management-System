namespace WMS.Application.Features.Inventory.Queries;

public sealed record TenantStockSummaryDto(
    int ProductId,
    string SKU,
    string ProductName,
    int ReorderPoint,
    decimal TotalQuantityOnHand,
    decimal TotalQuantityAllocated,
    decimal TotalAvailableQuantity,
    bool IsLowStock);
public record GetTenantStockSummaryQuery(
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PagedResult<TenantStockSummaryDto>>;
public class GetTenantStockSummaryQueryValidator : AbstractValidator<GetTenantStockSummaryQuery>
{
    public GetTenantStockSummaryQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
internal sealed class GetTenantStockSummaryQueryHandler(IWmsDbContext context, ITenantContext tenantContext)
    : IRequestHandler<GetTenantStockSummaryQuery, PagedResult<TenantStockSummaryDto>>
{
    public async Task<PagedResult<TenantStockSummaryDto>> Handle(
        GetTenantStockSummaryQuery request,
        CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to view inventory.");

        var stockSummary = context.StockLevels
                .AsNoTracking()
                .GroupBy(sl => sl.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalQuantityOnHand = g.Sum(x => x.QuantityOnHand),
                    TotalQuantityAllocated = g.Sum(x => x.QuantityAllocated)
                });

        var query =
            from stock in stockSummary
            join product in context.Products.AsNoTracking()
                on stock.ProductId equals product.Id
            select new
            {
                ProductId = product.Id,
                SKU = product.SKU,
                ProductName = product.Name,
                ReorderPoint = product.ReorderPoint,

                TotalQuantityOnHand = stock.TotalQuantityOnHand,
                TotalQuantityAllocated = stock.TotalQuantityAllocated,

                TotalAvailableQuantity =
                    stock.TotalQuantityOnHand -
                    stock.TotalQuantityAllocated
            };
        var search = request.SearchTerm?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(item => item.SKU.Contains(search) 
            || item.ProductName.Contains(search));

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.ProductName)
            .ThenBy(x => x.ProductId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new TenantStockSummaryDto(
                x.ProductId,
                x.SKU,
                x.ProductName,
                x.ReorderPoint,
                x.TotalQuantityOnHand,
                x.TotalQuantityAllocated,
                x.TotalAvailableQuantity,
                x.TotalAvailableQuantity <= x.ReorderPoint
                )).ToListAsync(cancellationToken);

        return new PagedResult<TenantStockSummaryDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
