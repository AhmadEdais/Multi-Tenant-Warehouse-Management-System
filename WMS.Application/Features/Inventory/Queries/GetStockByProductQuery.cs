namespace WMS.Application.Features.Inventory.Queries;

public record StockLevelDto(
    int ProductId,
    int LocationId,
    decimal QuantityOnHand,
    decimal QuantityAllocated,
    decimal AvailableQuantity,
    byte[] RowVersion);
public record GetStockByProductQuery(
    int ProductId, 
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PagedResult<StockLevelDto>>;
public class GetStockByProductQueryValidator : AbstractValidator<GetStockByProductQuery>
{
    public GetStockByProductQueryValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("ProductId must be greater than 0.");
    }
}
internal sealed class GetStockByProductQueryHandler(IWmsDbContext context) : IRequestHandler<GetStockByProductQuery,PagedResult<StockLevelDto>>
{
    public async Task<PagedResult<StockLevelDto>> Handle(GetStockByProductQuery request, CancellationToken cancellationToken)
    {
        var query = context.StockLevels
            .AsNoTracking()
            .Where(sl => sl.ProductId == request.ProductId);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.LocationId) 
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