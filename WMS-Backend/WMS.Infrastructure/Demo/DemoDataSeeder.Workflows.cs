using WMS.Application.Common.Operations;
using WMS.Application.Features.Inbound.Commands;
using WMS.Application.Features.Putaway.Commands;
using WMS.Application.Features.Outbound.Commands;
using WMS.Domain.Constants;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Demo;

internal sealed partial class DemoDataSeeder
{
    private async Task SeedPurchasesAsync(CancellationToken ct)
    {
        poSequence = PurchaseOrderSequence.Create(tenantId, 1000);
        context.PurchaseOrderSequences.Add(poSequence);
        foreach (var data in DemoDataset.Purchases)
        {
            var order = PurchaseOrder.Create(tenantId, warehouses[data.Warehouse].Id, suppliers[data.Supplier].Id,
                "PO-" + poSequence.ReserveNext().ToString("D6", System.Globalization.CultureInfo.InvariantCulture),
                DateTime.UtcNow.Date.AddDays(3), actor);
            foreach (var line in data.Lines)
                order.AddLine(products[line.Sku].Id, line.Expected, products[line.Sku].UnitCost);
            if (data.Status == PurchaseOrderStatus.Canceled) order.Cancel();
            else if (data.Status != PurchaseOrderStatus.Draft) order.MarkAsPending();
            order.Touch(actor, DateTime.UtcNow);
            purchases.Add(order);
            context.PurchaseOrders.Add(order);
        }
        await SaveAsync(ct);
    }

    private async Task ReceiveAsync(CancellationToken ct)
    {
        for (var index = 0; index < purchases.Count; index++)
        {
            var data = DemoDataset.Purchases[index];
            var order = purchases[index];
            var receipts = data.Lines.Where(l => l.Received > 0).Select(l =>
                new ReceivePurchaseOrderLineDto(order.Lines.Single(x => x.ProductId == products[l.Sku].Id).Id,
                    l.Received)).ToList();
            if (receipts.Count == 0) continue;
            var dock = locations[$"{data.Warehouse}-DOCK"];
            var stockByProduct = stock.Values.Where(s => s.LocationId == dock.Id).ToDictionary(s => s.ProductId);
            ReceivingOperations.Receive(context, order, dock, receipts, stockByProduct, tenantId, actor);
            foreach (var level in stockByProduct.Values) stock[(level.ProductId, level.LocationId)] = level;
        }
        await SaveAsync(ct);
    }

    private async Task PutawayAsync(CancellationToken ct)
    {
        foreach (var warehouse in warehouses.Keys)
        {
            var dock = locations[$"{warehouse}-DOCK"];
            foreach (var dockStock in stock.Values.Where(s => s.LocationId == dock.Id && s.QuantityOnHand > 0).ToList())
            {
                var data = DemoDataset.Products.Single(p => products[p.Sku].Id == dockStock.ProductId);
                var destinations = DemoDataset.Putaway(warehouse, data, dockStock.QuantityOnHand)
                    .Select(d => new PutawayDestinationDto(locations[$"{warehouse}-{d.Bin}"].Id, d.Quantity)).ToList();
                var levels = stock.Values.Where(s => s.ProductId == dockStock.ProductId).ToDictionary(s => s.LocationId);
                PutawayOperations.Transfer(context, tenantId, dockStock.ProductId, dock, locations.Values.ToList(),
                    destinations, levels, actor);
                foreach (var level in levels.Values) stock[(level.ProductId, level.LocationId)] = level;
            }
        }
        await SaveAsync(ct);
    }

    private async Task SeedSalesAsync(CancellationToken ct)
    {
        soSequence = SalesOrderSequence.Create(tenantId, 2000);
        context.SalesOrderSequences.Add(soSequence);
        foreach (var data in DemoDataset.Sales)
        {
            var order = SalesOrder.Create(tenantId, customers[data.Customer].Id, warehouses[data.Warehouse].Id,
                "SO-" + soSequence.ReserveNext().ToString("D6", System.Globalization.CultureInfo.InvariantCulture),
                DateTime.UtcNow.Date.AddDays(2), actor);
            foreach (var line in data.Lines)
                order.AddLine(products[line.Sku].Id, line.Quantity, products[line.Sku].UnitPrice);
            if (data.Status == SalesOrderStatus.Canceled) order.Cancel(actor, DateTime.UtcNow);
            sales.Add(order);
            context.SalesOrders.Add(order);
        }
        await SaveAsync(ct);
    }

    private async Task AllocateAsync(CancellationToken ct)
    {
        for (var index = 0; index < sales.Count; index++)
        {
            var data = DemoDataset.Sales[index];
            if (data.Status is SalesOrderStatus.Draft or SalesOrderStatus.Canceled) continue;
            var order = sales[index];
            if (!customers[data.Customer].IsActive || !warehouses[data.Warehouse].IsActive
                || data.Lines.Any(l => !products[l.Sku].IsActive))
                throw new InvalidOperationException("Inactive demo master data cannot be allocated.");
            var bins = locations.Values.Where(l => l.WarehouseId == order.WarehouseId
                && l.IsActive && l.LocationType == LocationTypes.Bin).Select(l => l.Id).ToHashSet();
            SalesOrderAllocationOperations.Allocate(order,
                stock.Values.Where(s => bins.Contains(s.LocationId) && s.AvailableQuantity > 0
                    && data.Lines.Any(l => products[l.Sku].Id == s.ProductId
                        && (l.AllocationBin is null || locations[$"{data.Warehouse}-{l.AllocationBin}"].Id == s.LocationId)))
                    .ToList(), actor);
        }
        await SaveAsync(ct);
    }

    private async Task PickupAsync(CancellationToken ct)
    {
        for (var index = 0; index < sales.Count; index++)
        {
            var data = DemoDataset.Sales[index];
            if (data.Status is not (SalesOrderStatus.Picked or SalesOrderStatus.Shipped)) continue;
            var order = sales[index];
            var requestLines = new List<CreateSalesOrderPickupLineDto>();
            foreach (var scenario in data.Lines.Where(l => l.Pickup > 0))
            {
                var remaining = scenario.Pickup;
                var line = order.Lines.Single(l => l.ProductId == products[scenario.Sku].Id);
                foreach (var allocation in line.Allocations.OrderBy(a => a.Id))
                {
                    var quantity = Math.Min(remaining, allocation.Quantity);
                    if (quantity > 0) requestLines.Add(new(allocation.Id, quantity));
                    remaining -= quantity;
                }
                if (remaining != 0) throw new InvalidOperationException("Demo pickup exceeds allocations.");
            }
            var allocations = order.Lines.SelectMany(l => l.Allocations).ToDictionary(a => a.Id);
            var reservations = SalesOrderPickupOperations.ValidateReservations(order, requestLines, allocations, []);
            var stockById = stock.Values.ToDictionary(s => s.Id);
            SalesOrderPickupOperations.ValidateStock(order, tenantId, requestLines, reservations,
                stockById.Values.Select(s => new SalesOrderPickupOperations.StockAtLocation(s,
                    locations.Values.Single(l => l.Id == s.LocationId))).ToList());
            SalesOrderPickupOperations.ExecutePickup(context, order, tenantId, actor,
                new CreateSalesOrderPickupCommand(order.Id, "Customer dispatch team", "Demo collection", requestLines),
                reservations, stockById);
        }
        await SaveAsync(ct);
    }
}
