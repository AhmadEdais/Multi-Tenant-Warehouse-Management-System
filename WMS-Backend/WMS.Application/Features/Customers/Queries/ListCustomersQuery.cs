namespace WMS.Application.Features.Customers.Queries;

public record CustomerListDto(
    int Id,
    string Code,
    string Name,
    string? ContactEmail,
    string PhoneNumber,
    string? Address,
    decimal CreditLimit);
public record ListCustomersQuery(
    string? SearchTerm,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PagedResult<CustomerListDto>>;
public class ListCustomersQueryValidator : AbstractValidator<ListCustomersQuery>
{
    public ListCustomersQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
internal sealed class ListCustomersQueryHandler(IWmsDbContext context) : IRequestHandler<ListCustomersQuery, PagedResult<CustomerListDto>>
{
    public async Task<PagedResult<CustomerListDto>> Handle(ListCustomersQuery request, CancellationToken cancellationToken)
    {
        var query = context.Customers.AsNoTracking();
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();
            query = query.Where(c =>
                c.Code.Contains(searchTerm) ||
                c.Name.Contains(searchTerm));
        }
        var totalItems = await query.CountAsync(cancellationToken);
        var customers = await query
            .OrderBy(c => c.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CustomerListDto(
                c.Id,
                c.Code,
                c.Name,
                c.ContactEmail,
                c.PhoneNumber,
                c.Address,
                c.CreditLimit ?? 0))
            .ToListAsync(cancellationToken);
        return new PagedResult<CustomerListDto>(customers, totalItems, request.PageNumber, request.PageSize);
    }
}