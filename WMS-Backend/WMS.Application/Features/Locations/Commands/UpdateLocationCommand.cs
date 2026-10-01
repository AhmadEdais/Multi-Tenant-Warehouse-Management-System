namespace WMS.Application.Features.Locations.Commands;

public record UpdateLocationCommand(
        [property: JsonIgnore] int Id,
        int? ParentLocationId,
        string Name,
        string? Barcode,
        decimal? MaxWeightCapacityKg) : IRequest;

public class UpdateLocationCommandValidator : AbstractValidator<UpdateLocationCommand>
{
    public UpdateLocationCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.ParentLocationId).GreaterThan(0).When(x => x.ParentLocationId.HasValue);
        RuleFor(x => x.Name).Must(name => !string.IsNullOrWhiteSpace(name)).MaximumLength(100);
        RuleFor(x => x.Barcode).MaximumLength(100);
        RuleFor(x => x.MaxWeightCapacityKg).GreaterThanOrEqualTo(0).When(x => x.MaxWeightCapacityKg.HasValue);
    }
}
internal class UpdateLocationCommandHandler(IWmsDbContext context) : IRequestHandler<UpdateLocationCommand>
{
    public async Task Handle(UpdateLocationCommand request, CancellationToken cancellationToken)
    {
        var location = await context.Locations
            .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Location with ID {request.Id} not found.");
        if (!location.IsActive)
            throw new ConflictException("Inactive locations cannot be edited.");

        await LocationHierarchy.ValidateParentAsync(
            context, location.WarehouseId, location.LocationType, request.ParentLocationId, cancellationToken);

        var name = request.Name.Trim();
        var barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim();

        if (barcode is not null)
        {
            var barcodeExists = await context.Locations
                .AnyAsync(l => l.WarehouseId == location.WarehouseId
                            && l.Barcode == barcode
                            && l.Id != request.Id, cancellationToken);

            if (barcodeExists)
            {
                throw new ConflictException("Barcode already exists in this warehouse.");
            }
        }

        location.Update(request.ParentLocationId, name, barcode, request.MaxWeightCapacityKg);

        await context.SaveChangesAsync(cancellationToken);
    }
}
