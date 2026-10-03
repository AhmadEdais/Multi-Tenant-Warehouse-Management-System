namespace WMS.Application.Features.Customers.Queries;

public record CustomerListDto(
    int Id,
    string Code,
    string Name,
    string? ContactEmail,
    string PhoneNumber,
    string? Address,
    decimal? CreditLimit,
    bool IsActive);
public record ListCustomersQuery(
    string? SearchTerm = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 50) : IRequest<PagedResult<CustomerListDto>>;
public class ListCustomersQueryValidator : AbstractValidator<ListCustomersQuery>
{
    public ListCustomersQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
internal sealed class ListCustomersQueryHandler(
    IWmsDbContext context,
    ITenantContext tenantContext,
    ICurrentUserService currentUser) : IRequestHandler<ListCustomersQuery, PagedResult<CustomerListDto>>
{
    public async Task<PagedResult<CustomerListDto>> Handle(ListCustomersQuery request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to view customers.");

        var query = context.Customers.AsNoTracking();
        var search = request.SearchTerm?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c =>
                c.Code.Contains(search) ||
                c.Name.Contains(search));
        }
        var canSeeInactive = currentUser.IsInRole(Roles.TenantAdmin)
            || currentUser.IsInRole(Roles.WarehouseManager);
        if (!canSeeInactive)
            query = query.Where(c => c.IsActive);
        else if (request.IsActive is bool isActive)
            query = query.Where(c => c.IsActive == isActive);

        var totalItems = await query.CountAsync(cancellationToken);
        var customers = await query
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CustomerListDto(
                c.Id,
                c.Code,
                c.Name,
                c.ContactEmail,
                c.PhoneNumber,
                c.Address,
                c.CreditLimit,
                c.IsActive))
            .ToListAsync(cancellationToken);
        return new PagedResult<CustomerListDto>(customers, totalItems, request.PageNumber, request.PageSize);
    }
}
