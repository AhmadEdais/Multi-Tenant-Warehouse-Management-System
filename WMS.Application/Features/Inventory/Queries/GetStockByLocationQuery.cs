namespace WMS.Application.Features.Inventory.Queries;

public sealed record GetStockByLocationQuery(
    int LocationId,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PagedResult<StockLevelDto>>;
public class GetStockByLocationQueryValidator : AbstractValidator<GetStockByLocationQuery>
{
       public GetStockByLocationQueryValidator()
       {
            RuleFor(x => x.LocationId).GreaterThan(0);
            RuleFor(x => x.PageNumber).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
       }
}
internal sealed class GetStockByLocationQueryHandler(IWmsDbContext context) : IRequestHandler<GetStockByLocationQuery, PagedResult<StockLevelDto>>
{
    public async Task<PagedResult<StockLevelDto>> Handle(GetStockByLocationQuery request, CancellationToken cancellationToken)
    {
        var query = context.StockLevels
            .AsNoTracking()
            .Where(sl => sl.LocationId == request.LocationId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(s => s.ProductId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new StockLevelDto(
                s.ProductId,
                s.LocationId,
                s.QuantityOnHand,
                s.QuantityAllocated,
                s.QuantityOnHand - s.QuantityAllocated,
                s.RowVersion))
            .ToListAsync(cancellationToken);

        return new PagedResult<StockLevelDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}