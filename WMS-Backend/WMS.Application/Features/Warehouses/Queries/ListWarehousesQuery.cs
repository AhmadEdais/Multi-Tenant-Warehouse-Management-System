namespace WMS.Application.Features.Warehouses.Queries
{
    public record WarehouseListDto(int Id, string Code, string Name, string? Address, bool IsActive, DateTime CreatedAtUtc);
    
    public record ListWarehousesQuery(string? Search = null, bool? IsActive = null, int PageNumber = 1, int PageSize = 10) : IRequest<PagedResult<WarehouseListDto>>;

    public sealed class ListWarehousesQueryValidator : AbstractValidator<ListWarehousesQuery>
    {
        public ListWarehousesQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThan(0);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
    
   internal sealed class ListWarehousesQueryHandler(IWmsDbContext context) : IRequestHandler<ListWarehousesQuery, PagedResult<WarehouseListDto>>
   {
        public async Task<PagedResult<WarehouseListDto>> Handle(ListWarehousesQuery request, CancellationToken cancellationToken)
        {
            var query = context.Warehouses.AsNoTracking();
            var search = request.Search?.Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(w => w.Code.Contains(search) || w.Name.Contains(search));
            }
            if (request.IsActive.HasValue)
            {
                query = query.Where(w => w.IsActive == request.IsActive.Value);
            }

            var totalItems = await query.CountAsync(cancellationToken);
            var warehouses = await query
                .OrderBy(w => w.Name)
                .ThenBy(w => w.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(w => new WarehouseListDto
                (
                    w.Id,
                    w.Code,
                    w.Name,
                    w.Address,
                    w.IsActive,
                    w.CreatedAtUtc
                ))
                .ToListAsync(cancellationToken);
            return new PagedResult<WarehouseListDto>(warehouses, totalItems, request.PageNumber, request.PageSize);
        }
    }
}
