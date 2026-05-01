namespace WMS.Application.Features.Warehouses.Queries
{
    public record WarehouseDto(
         int Id,
         string? Code,
         string? Name,
         string? Address,
         DateTime CreatedAtUtc
    );
    public record GetWarehouseByIdQuery(int Id) : IRequest<WarehouseDto>;
    public sealed class GetWarehouseByIdQueryValidator : AbstractValidator<GetWarehouseByIdQuery>
    {
        public  GetWarehouseByIdQueryValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id must be positive integer");
        }
    }
     public  sealed class GetWarehouseByIdQueryHandler : IRequestHandler<GetWarehouseByIdQuery, WarehouseDto> 
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
                (
                     w.Id,
                    w.Code,
                    w.Name,
                    w.Address,
                    w.CreatedAtUtc
                ))
                .FirstOrDefaultAsync(cancellationToken);
            return warehouse ?? throw new NotFoundException($"Warehouse with ID {request.Id} not found.");
        }
    }
}
