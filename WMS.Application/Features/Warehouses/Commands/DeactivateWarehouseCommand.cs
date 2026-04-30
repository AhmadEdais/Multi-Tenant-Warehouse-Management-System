namespace WMS.Application.Features.Warehouses.Commands
{
    public class DeactivateWarehouseCommand : IRequest
    {
        public int Id { get; set; }
    }
    internal class DeactivateWarehouseCommandValidator : AbstractValidator<DeactivateWarehouseCommand>
    {
        public DeactivateWarehouseCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Warehouse Id must be greater than 0.");
        }
    }
    internal class DeactivateWarehouseCommandHandler : IRequestHandler<DeactivateWarehouseCommand>
    {
        private readonly IWmsDbContext _dbContext;
        private readonly ITenantContext _tenantContext;
        public DeactivateWarehouseCommandHandler(IWmsDbContext dbContext, ITenantContext tenantContext)
        {
            _dbContext = dbContext;
            _tenantContext = tenantContext;
        }
        public async Task Handle(DeactivateWarehouseCommand request, CancellationToken cancellationToken)
        {
            var warehouse = await _dbContext.Warehouses
                .FirstOrDefaultAsync(w => w.Id == request.Id && w.TenantId == _tenantContext.TenantId, cancellationToken);
            if (warehouse == null)
            {
                throw new NotFoundException($"Warehouse with Id {request.Id} not found.");
            }
            warehouse.Deactivate();
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
