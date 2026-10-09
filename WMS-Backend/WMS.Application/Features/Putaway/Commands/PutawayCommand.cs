using WMS.Domain.Enums;

namespace WMS.Application.Features.Putaway.Commands;

public sealed record PutawayDestinationDto(int LocationId, decimal Quantity);

public sealed record PutawayCommand(
    int WarehouseId,
    int ProductId,
    List<PutawayDestinationDto> Destinations) : IRequest;

public sealed class PutawayCommandValidator : AbstractValidator<PutawayCommand>
{
    public PutawayCommandValidator()
    {
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.Destinations)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(destinations => destinations.All(destination => destination is not null
                && destination.LocationId > 0 && destination.Quantity > 0))
            .WithMessage("Each destination must have a valid LocationId and positive Quantity.")
            .Must(destinations => destinations.All(destination =>
                destination.Quantity == decimal.Round(destination.Quantity, 2)
                && destination.Quantity < 10000000000000000m))
            .WithMessage("Putaway quantities must fit decimal(18,2).")
            .Must(destinations => destinations.Select(destination => destination.LocationId).Distinct().Count()
                == destinations.Count)
            .WithMessage("A destination Bin can only appear once in a Putaway request.");
    }
}

internal sealed class PutawayCommandHandler(
    IWmsDbContext context,
    ITenantContext tenantContext,
    ICurrentUserService currentUser) : IRequestHandler<PutawayCommand>
{
    public async Task Handle(PutawayCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedAccessException("A tenant workspace is required to put away stock.");
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("A signed-in user is required to put away stock.");
        var actor = userId.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var warehouseExists = await context.Warehouses
            .AnyAsync(warehouse => warehouse.Id == request.WarehouseId, cancellationToken);
        if (!warehouseExists)
            throw new NotFoundException($"Warehouse with ID {request.WarehouseId} not found.");

        var productExists = await context.Products
            .AnyAsync(product => product.Id == request.ProductId, cancellationToken);
        if (!productExists)
            throw new NotFoundException($"Product with ID {request.ProductId} not found.");

        var docks = await context.Locations
            .Where(location => location.WarehouseId == request.WarehouseId
                && location.LocationType == LocationTypes.Dock)
            .ToListAsync(cancellationToken);
        if (docks.Count != 1 || !docks[0].IsActive || docks[0].ParentLocationId.HasValue)
            throw new ConflictException("The warehouse must have exactly one active root Receiving Dock.");
        var dock = docks[0];

        var destinationIds = request.Destinations.Select(destination => destination.LocationId).ToArray();
        var bins = await context.Locations
            .Where(location => destinationIds.Contains(location.Id))
            .ToListAsync(cancellationToken);
        if (bins.Count != destinationIds.Length || bins.Any(bin =>
                !bin.IsActive || bin.LocationType != LocationTypes.Bin
                || bin.WarehouseId != request.WarehouseId || bin.Id == dock.Id))
            throw new ConflictException("Every destination must be an active Bin in the selected warehouse.");

        var locationIds = destinationIds.Append(dock.Id).ToArray();
        var stockByLocation = await context.StockLevels
            .Where(stock => stock.ProductId == request.ProductId && locationIds.Contains(stock.LocationId))
            .ToDictionaryAsync(stock => stock.LocationId, cancellationToken);
        if (!stockByLocation.TryGetValue(dock.Id, out var dockStock))
            throw new ConflictException("No stock for this product is available in the Receiving Dock.");

        var totalQuantity = request.Destinations.Sum(destination => destination.Quantity);
        if (totalQuantity > dockStock.QuantityOnHand)
            throw new ConflictException("Putaway quantity exceeds the stock on hand in the Receiving Dock.");
        if (totalQuantity > dockStock.AvailableQuantity)
            throw new ConflictException("Allocated Receiving Dock stock cannot be put away.");

        var now = DateTime.UtcNow;
        dockStock.TransferOut(totalQuantity, actor, now);

        foreach (var destination in request.Destinations)
        {
            if (!stockByLocation.TryGetValue(destination.LocationId, out var binStock))
            {
                binStock = StockLevel.Create(tenantId, request.ProductId, destination.LocationId, actor, now);
                context.StockLevels.Add(binStock);
                stockByLocation.Add(destination.LocationId, binStock);
            }
            binStock.TransferIn(destination.Quantity, actor, now);

            // Each movement references the opposite location in its transfer pair.
            context.StockMovements.Add(StockMovement.Create(
                tenantId, request.ProductId, dock.Id, -destination.Quantity,
                MovementType.TransferOut, actor, now, "Locations", destination.LocationId));
            context.StockMovements.Add(StockMovement.Create(
                tenantId, request.ProductId, destination.LocationId, destination.Quantity,
                MovementType.TransferIn, actor, now, "Locations", dock.Id));
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
