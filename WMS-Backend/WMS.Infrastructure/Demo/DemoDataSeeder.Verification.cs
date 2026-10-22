using System.Diagnostics;
using Microsoft.Extensions.Logging;
using WMS.Domain.Constants;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Demo;

internal sealed partial class DemoDataSeeder
{
    private async Task VerifyConsistencyAsync(CancellationToken ct)
    {
        context.ChangeTracker.DetectChanges();
        Require(!context.ChangeTracker.HasChanges(), "All seeded changes must be saved before verification.");
        Require(context.ChangeTracker.Entries<IMustBelongToTenant>().All(e => e.Entity.TenantId == tenantId),
            "Seeded record tenant ownership");
        Require(warehouses.Count == 2 && locations.Values.Count(l => l.LocationType == LocationTypes.Bin) == 20,
            "Warehouse/bin counts");
        Require(categories.Count == 14 && categories.Values.Count(c => c.ParentCategoryId == null) == 4,
            "Category counts");
        Require(products.Count == 28 && suppliers.Count == 5 && customers.Count == 6, "Catalog counts");
        var warehouseIds = warehouses.Values.Select(w => w.Id).ToHashSet();
        var locationById = locations.Values.ToDictionary(l => l.Id);
        var productIds = products.Values.Select(p => p.Id).ToHashSet();
        var categoryIds = categories.Values.Select(c => c.Id).ToHashSet();
        foreach (var warehouse in warehouses.Values)
            Require(locations.Values.Count(l => l.WarehouseId == warehouse.Id && l.LocationType == LocationTypes.Dock
                && l.ParentLocationId == null && l.IsActive) == 1, "One active root dock per warehouse");
        foreach (var location in locations.Values)
        {
            Require(warehouseIds.Contains(location.WarehouseId), "Location warehouse ownership");
            var parent = location.ParentLocationId.HasValue ? locationById[location.ParentLocationId.Value] : null;
            Require(location.LocationType == LocationTypes.Dock ? parent is null :
                LocationTypes.RequiredParentType(location.LocationType) == parent?.LocationType,
                "Location parent type");
            Require(parent is null || parent.WarehouseId == location.WarehouseId, "Location parent warehouse");
        }
        foreach (var category in categories.Values)
            Require(!category.ParentCategoryId.HasValue || categoryIds.Contains(category.ParentCategoryId.Value),
                "Category parent ownership");
        var mappings = context.ChangeTracker.Entries<ProductCategory>().Select(e => e.Entity).ToList();
        Require(mappings.Count == 28 && mappings.All(m => productIds.Contains(m.ProductId)
            && categoryIds.Contains(m.CategoryId)), "Product category mappings");
        foreach (var data in DemoDataset.Products)
            Require(mappings.Any(m => m.ProductId == products[data.Sku].Id
                && m.CategoryId == categories[data.Category].Id), "Product leaf category");

        // Anonymous requests cannot see this tenant through the global filters. Every
        // verification query explicitly scopes to the newly provisioned tenant only.
        var queryTimer = Stopwatch.StartNew();
        var persistedStock = await context.StockLevels.IgnoreQueryFilters().AsNoTracking()
            .Where(s => s.TenantId == tenantId).ToListAsync(ct);
        var movements = await context.StockMovements.IgnoreQueryFilters().AsNoTracking()
            .Where(m => m.TenantId == tenantId).ToListAsync(ct);
        var pickups = await context.SalesOrderPickups.IgnoreQueryFilters().AsNoTracking()
            .Where(p => p.TenantId == tenantId).ToListAsync(ct);
        var pickupLines = await context.SalesOrderPickupLines.IgnoreQueryFilters().AsNoTracking()
            .Where(p => p.TenantId == tenantId).ToListAsync(ct);
        var persistedPoNumber = await context.PurchaseOrderSequences.IgnoreQueryFilters().AsNoTracking()
            .Where(s => s.TenantId == tenantId).Select(s => s.LastNumber).SingleAsync(ct);
        var persistedSoNumber = await context.SalesOrderSequences.IgnoreQueryFilters().AsNoTracking()
            .Where(s => s.TenantId == tenantId).Select(s => s.LastNumber).SingleAsync(ct);
        logger.LogInformation("[Demo Performance] Validation queries: {ElapsedMilliseconds:F2} ms",
            queryTimer.Elapsed.TotalMilliseconds);

        Require(purchases.Count == 10 && sales.Count == 10 && persistedPoNumber == 1010
            && persistedSoNumber == 2010 && poSequence.LastNumber == 1010 && soSequence.LastNumber == 2010,
            "Order sequence counters");
        for (var i = 0; i < purchases.Count; i++)
        {
            var order = purchases[i];
            Require(order.OrderNumber == $"PO-{1001 + i:D6}" && order.Status == DemoDataset.Purchases[i].Status,
                "Purchase order status/number");
            Require(warehouseIds.Contains(order.WarehouseId) && suppliers.Values.Any(s => s.Id == order.SupplierId),
                "Purchase order ownership");
            foreach (var line in order.Lines)
            {
                Require(line.PurchaseOrderId == order.Id && line.TenantId == tenantId
                    && productIds.Contains(line.ProductId) && line.ReceivedQuantity >= 0
                    && line.ReceivedQuantity <= line.ExpectedQuantity, "Purchase line integrity");
                var receipts = movements.Where(m => m.MovementType == MovementType.Receipt
                    && m.ReferenceTable == "PurchaseOrders" && m.ReferenceId == order.Id && m.ProductId == line.ProductId);
                Require(receipts.Sum(m => m.Quantity) == line.ReceivedQuantity
                    && receipts.All(m => locationById[m.LocationId].WarehouseId == order.WarehouseId
                        && locationById[m.LocationId].LocationType == LocationTypes.Dock), "Receipt reconciliation");
            }
            if (order.Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Pending or PurchaseOrderStatus.Canceled)
                Require(order.Lines.All(l => l.ReceivedQuantity == 0), "Unreceived purchase order");
            else if (order.Status == PurchaseOrderStatus.Received)
                Require(order.Lines.All(l => l.ReceivedQuantity == l.ExpectedQuantity), "Completed purchase order");
            else
                Require(order.Lines.Any(l => l.ReceivedQuantity > 0)
                    && order.Lines.Any(l => l.ReceivedQuantity < l.ExpectedQuantity), "Partial purchase order");
        }

        var stockById = persistedStock.ToDictionary(s => s.Id);
        var pickedByAllocation = pickupLines.GroupBy(p => p.SalesOrderAllocationId)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Quantity));
        var remainingByStock = new Dictionary<int, decimal>();
        var allAllocations = sales.SelectMany(o => o.Lines).SelectMany(l => l.Allocations).ToDictionary(a => a.Id);
        foreach (var pickup in pickups)
            Require(sales.Any(o => o.Id == pickup.SalesOrderId), "Pickup order ownership");
        foreach (var line in pickupLines)
        {
            Require(pickups.Any(p => p.Id == line.SalesOrderPickupId)
                && allAllocations.ContainsKey(line.SalesOrderAllocationId), "Pickup line ownership");
            var pickup = pickups.Single(p => p.Id == line.SalesOrderPickupId);
            var allocation = allAllocations[line.SalesOrderAllocationId];
            var orderLine = sales.Single(o => o.Id == pickup.SalesOrderId).Lines
                .SingleOrDefault(l => l.Id == allocation.SalesOrderLineId);
            Require(orderLine is not null, "Pickup allocation order");
            var shipment = movements.Where(m => m.SalesOrderPickupLineId == line.Id).ToList();
            Require(shipment.Count == 1 && shipment[0].MovementType == MovementType.Shipment
                && shipment[0].Quantity == -line.Quantity && shipment[0].ReferenceId == pickup.SalesOrderId
                && shipment[0].ProductId == orderLine!.ProductId
                && shipment[0].LocationId == stockById[allocation.StockLevelId].LocationId, "Shipment history");
        }
        for (var i = 0; i < sales.Count; i++)
        {
            var order = sales[i];
            Require(order.OrderNumber == $"SO-{2001 + i:D6}" && order.Status == DemoDataset.Sales[i].Status,
                "Sales order status/number");
            Require(warehouseIds.Contains(order.WarehouseId) && customers.Values.Any(c => c.Id == order.CustomerId),
                "Sales order ownership");
            decimal pickedOnOrder = 0;
            foreach (var line in order.Lines)
            {
                Require(line.SalesOrderId == order.Id && line.TenantId == tenantId && productIds.Contains(line.ProductId),
                    "Sales line ownership");
                decimal remainingOnLine = 0;
                decimal pickedOnLine = 0;
                foreach (var allocation in line.Allocations)
                {
                    var level = stockById[allocation.StockLevelId];
                    Require(allocation.TenantId == tenantId && allocation.SalesOrderLineId == line.Id
                        && level.ProductId == line.ProductId && locationById[level.LocationId].WarehouseId == order.WarehouseId
                        && locationById[level.LocationId].LocationType == LocationTypes.Bin, "Allocation ownership");
                    var picked = pickedByAllocation.GetValueOrDefault(allocation.Id);
                    var remaining = allocation.Quantity - picked;
                    Require(remaining >= 0 && (remaining == 0 ? allocation.Status == SalesOrderAllocationStatus.Shipped
                        : allocation.Status == SalesOrderAllocationStatus.Active), "Allocation status/history");
                    pickedOnLine += picked;
                    remainingOnLine += remaining;
                    remainingByStock[level.Id] = remainingByStock.GetValueOrDefault(level.Id) + remaining;
                }
                Require(remainingOnLine == line.QuantityAllocated, "Line reservation reconciliation");
                if (order.Status is SalesOrderStatus.Draft or SalesOrderStatus.Canceled)
                    Require(line.Allocations.Count == 0 && pickedOnLine == 0, "Unallocated order");
                else Require(pickedOnLine + remainingOnLine == line.Quantity, "Line fulfillment reconciliation");
                pickedOnOrder += pickedOnLine;
            }
            if (order.Status == SalesOrderStatus.Allocated) Require(pickedOnOrder == 0, "Allocated order pickups");
            if (order.Status == SalesOrderStatus.Picked) Require(pickedOnOrder > 0
                && order.Lines.Sum(l => l.QuantityAllocated) > 0, "Partial order pickups");
            if (order.Status == SalesOrderStatus.Shipped) Require(order.Lines.All(l => l.QuantityAllocated == 0),
                "Shipped order reservations");
        }
        Require(persistedStock.Count == stock.Count, "Stock persistence count");
        foreach (var level in persistedStock)
        {
            Require(productIds.Contains(level.ProductId) && locationById.ContainsKey(level.LocationId), "Stock ownership");
            Require(level.QuantityOnHand >= 0 && level.QuantityAllocated >= 0
                && level.QuantityAllocated <= level.QuantityOnHand, "Stock quantity bounds");
            Require(level.QuantityAllocated == remainingByStock.GetValueOrDefault(level.Id), "Bin reservation totals");
            Require(movements.Where(m => m.ProductId == level.ProductId && m.LocationId == level.LocationId)
                .Sum(m => m.Quantity) == level.QuantityOnHand, "Physical stock reconciliation");
        }
        foreach (var movement in movements)
        {
            Require(stock.ContainsKey((movement.ProductId, movement.LocationId)), "Movement stock ownership");
            if (movement.MovementType == MovementType.Receipt)
                Require(movement.Quantity > 0 && purchases.Any(p => p.Id == movement.ReferenceId), "Receipt reference");
            if (movement.MovementType == MovementType.Shipment)
                Require(movement.Quantity < 0 && pickupLines.Any(p => p.Id == movement.SalesOrderPickupLineId), "Shipment reference");
            if (movement.MovementType == MovementType.TransferOut)
                Require(movements.Any(other => other.MovementType == MovementType.TransferIn
                    && other.ProductId == movement.ProductId && other.LocationId == movement.ReferenceId
                    && other.ReferenceId == movement.LocationId && other.Quantity == -movement.Quantity), "Transfer pairing");
        }
        Require(movements.Where(m => m.MovementType is MovementType.TransferIn or MovementType.TransferOut)
            .Sum(m => m.Quantity) == 0, "Transfer conservation");
        foreach (var transfers in movements.Where(m => m.MovementType is MovementType.TransferIn or MovementType.TransferOut)
                     .GroupBy(m => (m.ProductId, Source: m.MovementType == MovementType.TransferOut
                         ? m.LocationId : m.ReferenceId, Destination: m.MovementType == MovementType.TransferIn
                         ? m.LocationId : m.ReferenceId)))
            Require(transfers.Sum(m => m.Quantity) == 0
                && transfers.Count(m => m.MovementType == MovementType.TransferOut)
                    == transfers.Count(m => m.MovementType == MovementType.TransferIn), "Transfer pair totals");
        Require(persistedStock.Where(s => s.QuantityOnHand > 0).GroupBy(s => s.ProductId)
            .Count(g => g.Select(s => locationById[s.LocationId].WarehouseId).Distinct().Count() == 2) >= 3,
            "Products stocked in both warehouses");
        Require(persistedStock.Where(s => s.QuantityOnHand > 0 && locationById[s.LocationId].LocationType == LocationTypes.Bin)
            .GroupBy(s => s.ProductId).Count(g => g.Count() > 1) >= 2, "Products occupying multiple bins");
        Require(persistedStock.Count(s => s.QuantityAllocated > 0) >= 3
            && movements.Any(m => m.MovementType == MovementType.Receipt)
            && movements.Any(m => m.MovementType == MovementType.Shipment), "Operational stock history");
        Require(persistedStock.Count(s => s.QuantityOnHand > 0 && locationById[s.LocationId].LocationType == LocationTypes.Dock) == 5,
            "Five putaway products");
        var totals = products.Values.Select(p => new { Product = p,
            OnHand = persistedStock.Where(s => s.ProductId == p.Id).Sum(s => s.QuantityOnHand),
            Available = persistedStock.Where(s => s.ProductId == p.Id).Sum(s => s.AvailableQuantity) }).ToList();
        Require(totals.Count(t => t.Available <= t.Product.ReorderPoint) is >= 3 and <= 4
            && totals.Any(t => t.OnHand == 0), "Low stock and sold-out examples");
        var scanner = persistedStock.Where(s => s.ProductId == products["SCN-001"].Id).ToList();
        Require(scanner.Sum(s => s.QuantityOnHand) == 95 && scanner.Sum(s => s.QuantityAllocated) == 3
            && scanner.Single(s => s.LocationId == locations["MAIN-A1-R1-B1"].Id).QuantityOnHand == 55
            && scanner.Single(s => s.LocationId == locations["MAIN-A1-R1-B2"].Id).QuantityOnHand == 30,
            "Scanner example reconciliation");
    }

    private static void Require(bool valid, string invariant)
    {
        if (!valid) throw new InvalidOperationException($"Demo consistency verification failed: {invariant}.");
    }
}
