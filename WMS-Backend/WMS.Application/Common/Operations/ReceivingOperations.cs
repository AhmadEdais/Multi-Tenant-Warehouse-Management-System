using WMS.Domain.Enums;
using WMS.Application.Features.Inbound.Commands;

namespace WMS.Application.Common.Operations;

public static class ReceivingOperations
{
    public static void Receive(IWmsDbContext context, PurchaseOrder po, Location dock,
        List<ReceivePurchaseOrderLineDto> receipts, Dictionary<int, StockLevel> stockByProduct,
        int tenantId, string actor)
    {
        if (po.TenantId != tenantId || dock.TenantId != tenantId || dock.WarehouseId != po.WarehouseId
            || dock.LocationType != LocationTypes.Dock || dock.ParentLocationId.HasValue || !dock.IsActive
            || po.Status is not (PurchaseOrderStatus.Pending or PurchaseOrderStatus.Receiving))
            throw new ConflictException("Invalid Purchase Order receiving context.");
        var poLines = po.Lines.ToDictionary(line => line.Id);
        foreach (var receipt in receipts)
            if (!poLines.TryGetValue(receipt.PurchaseOrderLineId, out var line)
                || line.TenantId != tenantId || receipt.Quantity <= 0
                || receipt.Quantity > line.ExpectedQuantity - line.ReceivedQuantity)
                throw new ConflictException("Invalid receipt quantity or line.");
        if (stockByProduct.Values.Any(stock => stock.TenantId != tenantId || stock.LocationId != dock.Id))
            throw new ConflictException("Receiving stock ownership is inconsistent.");
        var now = DateTime.UtcNow;

        foreach (var receipt in receipts)
        {
            var line = poLines[receipt.PurchaseOrderLineId];
            line.Receive(receipt.Quantity);

            if (!stockByProduct.TryGetValue(line.ProductId, out var stock))
            {
                stock = StockLevel.Create(tenantId, line.ProductId, dock.Id, actor, now);
                context.StockLevels.Add(stock);
                stockByProduct.Add(line.ProductId, stock);
            }
            stock.Receive(receipt.Quantity, actor, now);

            context.StockMovements.Add(StockMovement.Create(
                tenantId, line.ProductId, dock.Id, receipt.Quantity,
                MovementType.Receipt, actor, now, "PurchaseOrders", po.Id));
        }

        po.MarkAsReceiving();
        po.TryMarkAsFullyReceived();
        po.Touch(actor, now);
    }
}
