namespace WMS.Application.Features.Suppliers.Queries;

public record SupplierListDto(
    int Id,
    string Code,
    string Name,
    string? ContactEmail,
    string PhoneNumber,
    string? Address);
public record ListSuppliersQuery(
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 50) : IRequest<PagedResult<SupplierListDto>>;
internal class ListSuppliersQueryHandler(IWmsDbContext context) : IRequestHandler<ListSuppliersQuery, PagedResult<SupplierListDto>>
{
    public async Task<PagedResult<SupplierListDto>> Handle(ListSuppliersQuery request, CancellationToken cancellationToken)
    {
        var query = context.Suppliers
                    .AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();
            query = query.Where(c => c.Name.Contains(searchTerm) || c.Code.Contains(searchTerm));
        }
        var suppliers = await query
            .OrderBy(s => s.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new SupplierListDto(
                s.Id,
                s.Code,
                s.Name,
                s.ContactEmail,
                s.PhoneNumber,
                s.Address))
            .ToListAsync(cancellationToken);
        var totalSuppliers = await query.CountAsync(cancellationToken);
        return new PagedResult<SupplierListDto>(suppliers, totalSuppliers, request.PageNumber, request.PageSize);
    }
}