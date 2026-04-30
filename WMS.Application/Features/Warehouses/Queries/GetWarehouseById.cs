namespace WMS.Application.Features.Warehouses.Queries
{
    public class WarehouseDto
    {
    public int Id { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    }
    public class GetWarehouseByIdQuery : IRequest<WarehouseDto>
    {
        public int Id { get; set; }
    }
    internal class GetWarehouseByIdQueryValidator : AbstractValidator<GetWarehouseByIdQuery>
    {
        public  GetWarehouseByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be positive integer");
        }
    }
     public class GetWarehouseByIdQueryHandler : IRequestHandler<GetWarehouseByIdQuery, WarehouseDto> 
    {
        private readonly IWmsDbContext _context;
        private readonly ITenantContext _tenantContext;
        public GetWarehouseByIdQueryHandler(IWmsDbContext context, ITenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;
        }
        public async Task<WarehouseDto> Handle(GetWarehouseByIdQuery  request, CancellationToken cancellationToken)
        {
            var tenantId = _tenantContext.TenantId;
            var warehouse = await _context.Warehouses
                .Where(w => w.TenantId == tenantId && w.Id == request.Id)
                .Select(w => new WarehouseDto
                {
                    Id = w.Id,
                    Code = w.Code,
                    Name = w.Name,
                    Address = w.Address,
                    CreatedAtUtc = w.CreatedAtUtc
                })
                .FirstOrDefaultAsync(cancellationToken);
            return warehouse ?? throw new NotFoundException($"Warehouse with ID {request.Id} not found.");
        }
    }
}
