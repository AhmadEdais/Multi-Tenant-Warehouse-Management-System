namespace WMS.Application.Features.Warehouses.Queries
{
    public record WarehouseListDto(int Id, string? Code, string? Name, string? Address, DateTime CreatedAtUtc);
    
    public record ListWarehousesQuery(int PageNumber = 1, int PageSize = 10) : IRequest<PagedResult<WarehouseListDto>>;
    
   internal sealed class ListWarehousesQueryHandler(IWmsDbContext context) : IRequestHandler<ListWarehousesQuery, PagedResult<WarehouseListDto>>
   {
        private readonly IWmsDbContext _context = context;

        public async Task<PagedResult<WarehouseListDto>> Handle(ListWarehousesQuery request, CancellationToken cancellationToken)
        {
            var warehouses = await _context.Warehouses
                .AsNoTracking()
                .OrderBy(w => w.Name)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(w => new WarehouseListDto
                (
                    w.Id,
                    w.Code,
                    w.Name,
                    w.Address,
                    w.CreatedAtUtc
                ))
                .ToListAsync(cancellationToken);
            var totalItems = await _context.Warehouses.CountAsync(cancellationToken);
            return new PagedResult<WarehouseListDto>(warehouses, totalItems, request.PageNumber, request.PageSize);
        }
    }
}