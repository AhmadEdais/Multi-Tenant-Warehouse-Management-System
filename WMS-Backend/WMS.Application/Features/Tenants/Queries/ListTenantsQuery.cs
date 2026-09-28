namespace WMS.Application.Features.Tenants.Queries;

public record TenantDto(
    int Id,
    string Code,
    string Name,
    bool IsActive,
    DateTime CreatedAtUtc,
    int? CreatedByUserId);

public record ListTenantsQuery(int PageNumber = 1, int PageSize = 10, string? Search = null, bool? IsActive = null)
    : IRequest<PagedResult<TenantDto>>;

public sealed class ListTenantsQueryValidator : AbstractValidator<ListTenantsQuery>
{
    public ListTenantsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

internal sealed class ListTenantsQueryHandler(IWmsDbContext context)
    : IRequestHandler<ListTenantsQuery, PagedResult<TenantDto>>
{
    public async Task<PagedResult<TenantDto>> Handle(ListTenantsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Tenants.AsNoTracking();
        var search = request.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(t => t.Name.Contains(search) || t.Code.Contains(search));
        }

        if (request.IsActive is bool isActive)
        {
            query = query.Where(t => t.IsActive == isActive);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var tenants = await query
            .OrderBy(t => t.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TenantDto(t.Id, t.Code, t.Name, t.IsActive, t.CreatedAtUtc, t.CreatedByUserId))
            .ToListAsync(cancellationToken);

        return new PagedResult<TenantDto>(tenants, totalCount, request.PageNumber, request.PageSize);
    }
}
