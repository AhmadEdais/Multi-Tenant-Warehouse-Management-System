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
        RuleFor(x => x.LocationType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Barcode).MaximumLength(100);
        RuleFor(x => x.MaxWeightCapacityKg).GreaterThanOrEqualTo(0);
    }
}   
internal class CreateLocationCommandHandler(IWmsDbContext context) : IRequestHandler<CreateLocationCommand, int>
{
    public async Task<int> Handle(CreateLocationCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Barcode))
        {
            var barcodeExists = await context.Locations
            .AnyAsync(l => l.WarehouseId == request.WarehouseId && l.Barcode == request.Barcode, cancellationToken);
            if (barcodeExists)
            {
                throw new ConflictException($"A location with barcode '{request.Barcode}' already exists in the warehouse.");
            }
        }
        if (request.ParentLocationId.HasValue)
        {
            var parentLocationExists = await context.Locations
                .AnyAsync(l => l.Id == request.ParentLocationId && l.WarehouseId == request.WarehouseId, cancellationToken);
            if (!parentLocationExists)
            {
                throw new NotFoundException($"Parent location with ID '{request.ParentLocationId}' does not exist in the warehouse.");
            }
        }
        var location = Location.Create(
            request.WarehouseId,
            request.ParentLocationId,
            request.LocationType,
            request.Name,
            request.Barcode,
            request.MaxWeightCapacityKg);
        context.Locations.Add(location);
        await context.SaveChangesAsync(cancellationToken);
        return location.Id;
    }
}
