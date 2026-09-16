namespace WMS.Application.Features.Inventory.Queries;

public sealed record TenantStockSummaryDto(
    int ProductId,
    decimal TotalQuantityOnHand,
    decimal TotalQuantityAllocated,
    decimal TotalAvailableQuantity);
public record GetTenantStockSummaryQuery(
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
internal sealed class GetTenantStockSummaryQueryHandler(IWmsDbContext context) : IRequestHandler<GetTenantStockSummaryQuery, PagedResult<TenantStockSummaryDto>>
{
    public async Task<PagedResult<TenantStockSummaryDto>> Handle(GetTenantStockSummaryQuery request, CancellationToken cancellationToken)
    {
        var query = context.StockLevels
            .AsNoTracking();
        var totalCount = await query
            .Select(sl => sl.ProductId)
            .Distinct().
            CountAsync(cancellationToken);

        var items = await query
            .GroupBy(sl => sl.ProductId)
            .Select(s => new TenantStockSummaryDto(
                s.Key,
                s.Sum(x => x.QuantityOnHand),
                s.Sum(x => x.QuantityAllocated),
                s.Sum(x => x.QuantityOnHand) - s.Sum(x => x.QuantityAllocated)))
            .OrderBy(dto => dto.ProductId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TenantStockSummaryDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}