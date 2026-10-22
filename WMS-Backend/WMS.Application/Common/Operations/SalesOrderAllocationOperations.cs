namespace WMS.Application.Common.Operations;

public static class SalesOrderAllocationOperations
{
    // Callers load active products/customer/warehouse and eligible Bin stock before this operation.
    public static void Allocate(SalesOrder order, IReadOnlyCollection<StockLevel> stockLevels, string actor)
    {
        if (stockLevels.Any(stock => stock.TenantId != order.TenantId))
            throw new ConflictException("Allocation stock belongs to another tenant.");
        var stockByProduct = stockLevels.GroupBy(stock => stock.ProductId)
            .ToDictionary(group => group.Key, group => group.OrderBy(stock => stock.LocationId).ToList());

        // Check every line before changing tracked stock or the order.
        foreach (var line in order.Lines)
        {
            var available = stockByProduct.TryGetValue(line.ProductId, out var levels)
                ? levels.Sum(stock => stock.AvailableQuantity) : 0m;
            if (available < line.Quantity)
                throw new ConflictException($"Insufficient Bin stock to allocate product {line.ProductId}.");
        }

        var now = DateTime.UtcNow;
        foreach (var line in order.Lines)
        {
            var remaining = line.Quantity;
            foreach (var stock in stockByProduct[line.ProductId])
            {
                if (remaining == 0) break;
                var quantity = Math.Min(remaining, stock.AvailableQuantity);
                if (quantity <= 0) continue;
                line.Allocate(stock, quantity, actor, now);
                remaining -= quantity;
            }
        }

        order.MarkAllocated(actor, now);
    }
}
