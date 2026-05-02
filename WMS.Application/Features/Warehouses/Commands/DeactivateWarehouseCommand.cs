namespace WMS.Application.Features.Warehouses.Commands
{
    public record DeactivateWarehouseCommand(int Id) : IRequest;
    public sealed class DeactivateWarehouseCommandValidator : AbstractValidator<DeactivateWarehouseCommand>
    {
        public DeactivateWarehouseCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Warehouse Id must be greater than 0.");
        }
    }
    internal sealed class DeactivateWarehouseCommandHandler(IWmsDbContext dbContext) : IRequestHandler<DeactivateWarehouseCommand>
    {
        private readonly IWmsDbContext _dbContext = dbContext;

        public async Task Handle(DeactivateWarehouseCommand request, CancellationToken cancellationToken)
        {
            var warehouse = await _dbContext.Warehouses
                .FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException($"Warehouse with Id {request.Id} not found.");
            warehouse.Deactivate();
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
