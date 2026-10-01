namespace WMS.Application.Features.Categories.Queries;

public record CategoryDto(int Id, string Name, int? ParentCategoryId, bool IsActive);

public record ListCategoriesQuery(
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 50) : IRequest<PagedResult<CategoryDto>>;

public class ListCategoriesQueryValidator : AbstractValidator<ListCategoriesQuery>
{
    public ListCategoriesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
internal class ListCategoriesQueryHandler(IWmsDbContext context, ITenantContext tenantContext, ICurrentUserService currentUser) : IRequestHandler<ListCategoriesQuery, PagedResult<CategoryDto>>
{
    public async Task<PagedResult<CategoryDto>> Handle(ListCategoriesQuery request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to view categories.");
        var query = context.Categories
            .AsNoTracking();
        if (!currentUser.IsInRole(Roles.TenantAdmin) && !currentUser.IsInRole(Roles.WarehouseManager))
            query = query.Where(c => c.IsActive);
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();
            query = query.Where(c => c.Name.Contains(searchTerm));
        }
        var totalItems = await query.CountAsync(cancellationToken);
        var categories = await query
            .OrderBy(c => c.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CategoryDto(c.Id, c.Name, c.ParentCategoryId, c.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResult<CategoryDto>(categories, totalItems, request.PageNumber, request.PageSize);
    }
}
