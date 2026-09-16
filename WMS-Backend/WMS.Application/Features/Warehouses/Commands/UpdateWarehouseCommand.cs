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
    internal sealed class UpdateWarehouseCommandHandler(IWmsDbContext context) : IRequestHandler<UpdateWarehouseCommand>
    {
        public async Task Handle(UpdateWarehouseCommand request, CancellationToken cancellationToken)
        {
            var warehouse = await context.Warehouses
                .Where(w => w.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException($"Warehouse with ID {request.Id} not found.");
            warehouse.Update(request.Name, request.Address);
            await context.SaveChangesAsync(cancellationToken);

        }
    }
}