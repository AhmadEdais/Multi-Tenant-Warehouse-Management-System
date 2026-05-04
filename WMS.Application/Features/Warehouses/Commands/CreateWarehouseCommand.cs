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
    internal sealed class  CreateWarehouseCommandHandler(IWmsDbContext context, ITenantContext tenantContext) : IRequestHandler<CreateWarehouseCommand,int>
    {
        public async Task<int> Handle(CreateWarehouseCommand request, CancellationToken cancellationToken)
        {
            var tenantId = tenantContext.TenantId;
            if(!tenantId.HasValue)
            {
                throw new UnauthorizedAccessException("You must be logged into a specific tenant workspace to create a warehouse.");
            }

            var exists = await context.Warehouses
                .IgnoreQueryFilters()
                .AnyAsync(w => w.TenantId == tenantId && w.Code == request.Code, cancellationToken);

            if (exists)
            {
                throw new ConflictException($"A warehouse with code '{request.Code}' already exists for this tenant.");
            }
            var warehouse =  Warehouse.Create(tenantId.Value, request.Code, request.Name, request.Address);
            
            context.Warehouses.Add(warehouse);
            await context.SaveChangesAsync(cancellationToken);
            return warehouse.Id;
        }
    }
}