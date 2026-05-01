using System.Text.Json.Serialization;

namespace WMS.Application.Features.Warehouses.Commands
{
    public record UpdateWarehouseCommand(
    [property: JsonIgnore] int Id,
    string Name,
    string? Address) : IRequest;
    public sealed class UpdateWarehouseCommandValidator : AbstractValidator<UpdateWarehouseCommand>
    {
        public UpdateWarehouseCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Address).MaximumLength(500);
        }
    }
    internal sealed class UpdateWarehouseCommandHandler : IRequestHandler<UpdateWarehouseCommand>
    {
        private readonly IWmsDbContext _context;
        private readonly ITenantContext _tenantContext;

        public UpdateWarehouseCommandHandler(IWmsDbContext context, ITenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;
        }

        public async Task Handle(UpdateWarehouseCommand request, CancellationToken cancellationToken)
        {
            var tenantId = _tenantContext.TenantId;
            var warehouse = await _context.Warehouses
                .Where(w => w.TenantId == tenantId && w.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException($"Warehouse with ID {request.Id} not found.");
            warehouse.Update(request.Name, request.Address);
            await _context.SaveChangesAsync(cancellationToken);

        }
    }
}