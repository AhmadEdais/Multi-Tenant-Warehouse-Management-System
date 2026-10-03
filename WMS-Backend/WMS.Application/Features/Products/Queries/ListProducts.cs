namespace WMS.Application.Features.Products.Queries;
public record ProductDto(
        int Id,
        string SKU,
        string Name,
        decimal UnitPrice,
        int ReorderPoint,
        bool IsActive,
        List<int> CategoryIds);

public record ListProductsQuery(
    string? SearchTerm = null,
    bool? IsActive = null,
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
public class ListProductsQueryHandler(IWmsDbContext context, ICurrentUserService currentUser) : IRequestHandler<ListProductsQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Products.AsNoTracking();
        var search = request.SearchTerm?.Trim();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.SKU.Contains(search) || p.Name.Contains(search));
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == request.CategoryId.Value));
        }
        var canSeeInactiveProducts = currentUser.IsInRole(Roles.TenantAdmin)
            || currentUser.IsInRole(Roles.WarehouseManager);

        if(!canSeeInactiveProducts)
        {
            query = query.Where(p => p.IsActive);
        }
        else if (request.IsActive is bool isActive)
        {
            query = query.Where(p => p.IsActive == isActive);
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
                p.IsActive,
                p.ProductCategories.Select(pc => pc.CategoryId).ToList()
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
