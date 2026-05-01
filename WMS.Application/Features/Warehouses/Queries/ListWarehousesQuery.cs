namespace WMS.Application.Features.Warehouses.Queries
{
    public record WarehouseListDto(int Id, string? Code, string? Name, string? Address, DateTime CreatedAtUtc);
    
    public record ListWarehousesQuery(int PageNumber = 1, int PageSize = 10) : IRequest<List<WarehouseListDto>>;
    
   internal sealed class ListWarehousesQueryHandler : IRequestHandler<ListWarehousesQuery, List<WarehouseListDto>>
   {
        private readonly IWmsDbContext _context;
        private readonly ITenantContext _tenantContext;
        public ListWarehousesQueryHandler(IWmsDbContext context, ITenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;
        }
        public async Task<List<WarehouseListDto>> Handle(ListWarehousesQuery request, CancellationToken cancellationToken)
        {
            var tenantId = _tenantContext.TenantId;
            var warehouses = await _context.Warehouses
                .AsNoTracking()
                .Where(w => w.TenantId == tenantId)
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
            return warehouses;
        }
    }
}