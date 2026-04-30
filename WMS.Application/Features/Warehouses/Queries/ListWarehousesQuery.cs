namespace WMS.Application.Features.Warehouses.Queries
{
    public class WarehouseListDto
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAtUtc { get; set; }

    }
    public class ListWarehousesQuery : IRequest<List<WarehouseListDto>>
    {
        public int PageNumber { get; set; } = 1; 
        public int PageSize { get; set; } = 10;
    }
   internal class ListWarehousesQueryHandler : IRequestHandler<ListWarehousesQuery, List<WarehouseListDto>>
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
                {
                    Id = w.Id,
                    Code = w.Code,
                    Name = w.Name,
                    Address = w.Address,
                    CreatedAtUtc = w.CreatedAtUtc
                })
                .ToListAsync(cancellationToken);
            return warehouses;
        }
    }
}