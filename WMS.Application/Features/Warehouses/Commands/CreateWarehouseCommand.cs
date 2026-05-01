namespace WMS.Application.Features.Warehouses.Commands
{
    public record CreateWarehouseCommand(string Code, string Name, string? Address) : IRequest<int>;
    public sealed  class CreateWarehouseCommandValidator : AbstractValidator<CreateWarehouseCommand>
    {
        private readonly IWmsDbContext _context;
        public CreateWarehouseCommandValidator(IWmsDbContext context) 
        {
            _context = context;
            
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Code is required.")
                .MaximumLength(20).WithMessage("Code cannot exceed 20 characters.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(200).WithMessage("Name cannot exceed 200 characters.");
            RuleFor(x => x.Address)
                .MaximumLength(500).WithMessage("Address cannot exceed 500 characters.");
        }

    }
    internal sealed class  CreateWarehouseCommandHandler : IRequestHandler<CreateWarehouseCommand,int>
    {
        private readonly IWmsDbContext _context;
        private readonly ITenantContext _tenantContext;

        public CreateWarehouseCommandHandler(IWmsDbContext context, ITenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;

        }
        public async Task<int> Handle(CreateWarehouseCommand request, CancellationToken cancellationToken)
        {
            var tenantId = _tenantContext.TenantId;
            var exists = await _context.Warehouses
            .AnyAsync(w => w.TenantId == tenantId && w.Code == request.Code, cancellationToken);

            if (exists)
            {
                throw new ConflictException($"A warehouse with code '{request.Code}' already exists for this tenant.");
            }
            var warehouse =  Warehouse.Create(tenantId, request.Code, request.Name, request.Address);
            
            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync(cancellationToken);
            return warehouse.Id;
        }
    }
}