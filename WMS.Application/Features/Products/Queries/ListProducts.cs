namespace WMS.Application.Features.Products.Queries;
public record ProductDto(
        int Id,
        string SKU,
        string Name,
        decimal UnitPrice,
        int ReorderPoint,
        List<int> CategoryIds);

public record ListProductsQuery(
    string? SearchTerm = null,
    int? CategoryId = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<ProductDto>>;
public class ListProductsQueryValidator : AbstractValidator<ListProductsQuery>
{
    public ListProductsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
public class ListProductsQueryHandler(IWmsDbContext context) : IRequestHandler<ListProductsQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(p => p.SKU.Contains(request.SearchTerm) || p.Name.Contains(request.SearchTerm));
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == request.CategoryId.Value));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductDto(
                p.Id,
                p.SKU,
                p.Name,
                p.UnitPrice,
                p.ReorderPoint,
                p.ProductCategories.Select(pc => pc.CategoryId).ToList()
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
