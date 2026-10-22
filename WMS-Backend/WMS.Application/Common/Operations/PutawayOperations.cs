using WMS.Domain.Enums;
using WMS.Application.Features.Putaway.Commands;

namespace WMS.Application.Common.Operations;

public static class PutawayOperations
{
    public static void Transfer(IWmsDbContext context, int tenantId, int productId, Location dock,
        IReadOnlyCollection<Location> bins, List<PutawayDestinationDto> destinations,
        Dictionary<int, StockLevel> stockByLocation, string actor)
    {
        if (dock.TenantId != tenantId || dock.LocationType != LocationTypes.Dock || !dock.IsActive
            || dock.ParentLocationId.HasValue || destinations.Count == 0
            || destinations.Select(d => d.LocationId).Distinct().Count() != destinations.Count
            || destinations.Any(d => d.Quantity <= 0 || !bins.Any(bin => bin.Id == d.LocationId
                && bin.TenantId == tenantId && bin.WarehouseId == dock.WarehouseId
                && bin.LocationType == LocationTypes.Bin && bin.IsActive)))
            throw new ConflictException("Invalid putaway locations or quantities.");
        if (!stockByLocation.TryGetValue(dock.Id, out var dockStock))
            throw new ConflictException("No stock for this product is available in the Receiving Dock.");
        if (stockByLocation.Values.Any(stock => stock.TenantId != tenantId || stock.ProductId != productId))
            throw new ConflictException("Putaway stock ownership is inconsistent.");
        var totalQuantity = destinations.Sum(destination => destination.Quantity);
        if (totalQuantity > dockStock.QuantityOnHand)
            throw new ConflictException("Putaway quantity exceeds the stock on hand in the Receiving Dock.");
        if (totalQuantity > dockStock.AvailableQuantity)
            throw new ConflictException("Allocated Receiving Dock stock cannot be put away.");

        var now = DateTime.UtcNow;
        dockStock.TransferOut(totalQuantity, actor, now);

        foreach (var destination in destinations)
        {
            if (!stockByLocation.TryGetValue(destination.LocationId, out var binStock))
            {
                binStock = StockLevel.Create(tenantId, productId, destination.LocationId, actor, now);
                context.StockLevels.Add(binStock);
                stockByLocation.Add(destination.LocationId, binStock);
            }
            binStock.TransferIn(destination.Quantity, actor, now);

            // Each movement references the opposite location in its transfer pair.
            context.StockMovements.Add(StockMovement.Create(
                tenantId, productId, dock.Id, -destination.Quantity,
                MovementType.TransferOut, actor, now, "Locations", destination.LocationId));
            context.StockMovements.Add(StockMovement.Create(
                tenantId, productId, destination.LocationId, destination.Quantity,
                MovementType.TransferIn, actor, now, "Locations", dock.Id));
        }

    }
}
