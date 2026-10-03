namespace WMS.Application.Features.Suppliers.Queries;

public record SupplierListDto(
    int Id,
    string Code,
    string Name,
    string? ContactEmail,
    string PhoneNumber,
    string? Address,
    bool IsActive);
public record ListSuppliersQuery(
    string? SearchTerm = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 50) : IRequest<PagedResult<SupplierListDto>>;
public sealed class ListSuppliersQueryValidator : AbstractValidator<ListSuppliersQuery>
{
    public ListSuppliersQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

internal class ListSuppliersQueryHandler(
    IWmsDbContext context,
    ITenantContext tenantContext,
    ICurrentUserService currentUser) : IRequestHandler<ListSuppliersQuery, PagedResult<SupplierListDto>>
{
    public async Task<PagedResult<SupplierListDto>> Handle(ListSuppliersQuery request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to view suppliers.");

        var query = context.Suppliers.AsNoTracking();
        var search = request.SearchTerm?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.Code.Contains(search) || s.Name.Contains(search));
        }
        var canSeeInactive = currentUser.IsInRole(Roles.TenantAdmin)
            || currentUser.IsInRole(Roles.WarehouseManager);
        if (!canSeeInactive)
            query = query.Where(s => s.IsActive);
        else if (request.IsActive is bool isActive)
            query = query.Where(s => s.IsActive == isActive);

        var totalSuppliers = await query.CountAsync(cancellationToken);
        var suppliers = await query
            .OrderBy(s => s.Name)
            .ThenBy(s => s.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new SupplierListDto(
                s.Id,
                s.Code,
                s.Name,
                s.ContactEmail,
                s.PhoneNumber,
                s.Address,
                s.IsActive))
            .ToListAsync(cancellationToken);
        return new PagedResult<SupplierListDto>(suppliers, totalSuppliers, request.PageNumber, request.PageSize);
    }
}
