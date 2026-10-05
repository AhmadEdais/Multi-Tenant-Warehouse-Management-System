namespace WMS.Application.Features.Locations.Commands;

public record CreateLocationCommand(
    int WarehouseId,
    int? ParentLocationId,
    string LocationType,
    string Name,
    string? Barcode,
    decimal? MaxWeightCapacityKg) : IRequest<int>;

public class CreateLocationCommandValidator : AbstractValidator<CreateLocationCommand>
{
    public CreateLocationCommandValidator()
    {
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.ParentLocationId).GreaterThan(0).When(x => x.ParentLocationId.HasValue);
        RuleFor(x => x.LocationType)
            .Must(type => LocationTypes.IsAddable(type?.Trim()))
            .WithMessage("Location type must be Zone, Aisle, Rack, or Bin.");
        RuleFor(x => x.Name).Must(name => !string.IsNullOrWhiteSpace(name)).MaximumLength(100);
        RuleFor(x => x.Barcode).MaximumLength(100);
        RuleFor(x => x.MaxWeightCapacityKg).GreaterThanOrEqualTo(0);
    }
}   
internal class CreateLocationCommandHandler(IWmsDbContext context) : IRequestHandler<CreateLocationCommand, int>
{
    public async Task<int> Handle(CreateLocationCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await context.Warehouses
            .AsNoTracking()
            .Where(w => w.Id == request.WarehouseId)
            .Select(w => new { w.IsActive })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Warehouse was not found.");
        if (!warehouse.IsActive)
            throw new ConflictException("Cannot add a location to an inactive warehouse.");

        var locationType = request.LocationType.Trim();
        var name = request.Name.Trim();
        var barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim();
        await LocationHierarchy.ValidateParentAsync(
            context, request.WarehouseId, locationType, request.ParentLocationId, cancellationToken);

        if (barcode is not null)
        {
            var barcodeExists = await context.Locations
                .AnyAsync(l => l.WarehouseId == request.WarehouseId && l.Barcode == barcode, cancellationToken);
            if (barcodeExists)
            {
                throw new ConflictException("Barcode already exists in this warehouse.");
            }
        }
        var location = Location.Create(
            request.WarehouseId,
            request.ParentLocationId,
            locationType,
            name,
            barcode,
            request.MaxWeightCapacityKg);
        context.Locations.Add(location);
        await context.SaveChangesAsync(cancellationToken);
        return location.Id;
    }
}
