using WMS.Domain.Enums;
using WMS.Application.Features.Outbound.Commands;

namespace WMS.Application.Common.Operations;

public static class SalesOrderPickupOperations
{
    public static ReservationData ValidateReservations(
        SalesOrder order, List<CreateSalesOrderPickupLineDto> requestLines,
        Dictionary<long, SalesOrderAllocation> allocationById,
        Dictionary<long, decimal> pickedByAllocation)
    {
        decimal totalPreviouslyPicked = 0;
        decimal totalRemaining = 0;
        foreach (var line in order.Lines)
        {
            if (line.Quantity <= 0 || line.QuantityAllocated < 0 || line.QuantityAllocated > line.Quantity
                || line.Allocations.Count == 0)
                throw new ConflictException("Sales Order line reservations are inconsistent.");

            decimal pickedOnLine = 0;
            decimal reservedOnLine = 0;
            foreach (var allocation in line.Allocations)
            {
                if (allocation.SalesOrderLineId != line.Id || allocation.Quantity <= 0)
                    throw new ConflictException("Sales Order allocations are inconsistent.");
                var picked = pickedByAllocation.GetValueOrDefault(allocation.Id);
                var remaining = allocation.Quantity - picked;
                if (picked < 0 || remaining < 0
                    || allocation.Status is not (SalesOrderAllocationStatus.Active or SalesOrderAllocationStatus.Shipped)
                    || allocation.Status == SalesOrderAllocationStatus.Active && remaining <= 0
                    || allocation.Status == SalesOrderAllocationStatus.Shipped && remaining != 0)
                    throw new ConflictException("Sales Order allocation pickup history is inconsistent.");
                pickedOnLine += picked;
                if (allocation.Status == SalesOrderAllocationStatus.Active)
                    reservedOnLine += remaining;
            }
            if (line.QuantityAllocated != reservedOnLine || line.Quantity != pickedOnLine + reservedOnLine)
                throw new ConflictException("Sales Order remaining reservations are inconsistent.");
            totalPreviouslyPicked += pickedOnLine;
            totalRemaining += reservedOnLine;
        }
        if (order.Status == SalesOrderStatus.Allocated && totalPreviouslyPicked != 0
            || order.Status == SalesOrderStatus.Picked && totalPreviouslyPicked <= 0
            || totalRemaining <= 0)
            throw new ConflictException("Sales Order status does not match its pickup history.");

        var lineByAllocation = order.Lines
            .SelectMany(line => line.Allocations.Select(allocation => new { allocation.Id, Line = line }))
            .ToDictionary(row => row.Id, row => row.Line);
        foreach (var item in requestLines)
        {
            if (!allocationById.TryGetValue(item.SalesOrderAllocationId, out var allocation)
                || allocation.Status != SalesOrderAllocationStatus.Active
                || item.Quantity > allocation.Quantity - pickedByAllocation.GetValueOrDefault(allocation.Id))
                throw new ConflictException("A pickup allocation is unavailable or its remaining quantity is insufficient.");
        }
        foreach (var group in requestLines.GroupBy(item => lineByAllocation[item.SalesOrderAllocationId].Id))
        {
            var line = order.Lines.Single(candidate => candidate.Id == group.Key);
            if (group.Sum(item => item.Quantity) > line.QuantityAllocated)
                throw new ConflictException("Pickup exceeds the line's remaining reservation.");
        }

        return new ReservationData(allocationById, lineByAllocation, pickedByAllocation);
    }

    public sealed record StockAtLocation(StockLevel Stock, Location Location);

    public static Dictionary<int, StockLevel> ValidateStock(SalesOrder order, int tenantId,
        List<CreateSalesOrderPickupLineDto> requestLines, ReservationData reservations,
        IReadOnlyCollection<StockAtLocation> stockRows)
    {
        var allocationById = reservations.AllocationsById;
        var lineByAllocation = reservations.LinesByAllocation;
        var stockIds = requestLines.Select(item => allocationById[item.SalesOrderAllocationId].StockLevelId)
            .Distinct().ToArray();
        if (stockIds.Any(id => !stockRows.Any(row => row.Stock.Id == id)))
            throw new ConflictException("One or more reserved stock records are unavailable.");
        var stockById = stockRows.Where(row => stockIds.Contains(row.Stock.Id)).ToDictionary(row => row.Stock.Id);
        foreach (var item in requestLines)
        {
            var allocation = allocationById[item.SalesOrderAllocationId];
            var row = stockById[allocation.StockLevelId];
            if (row.Stock.TenantId != tenantId || row.Location.TenantId != tenantId
                || row.Stock.ProductId != lineByAllocation[allocation.Id].ProductId
                || row.Location.WarehouseId != order.WarehouseId
                || row.Location.LocationType != LocationTypes.Bin)
                throw new ConflictException("Reserved stock does not belong to this product and warehouse Bin.");
        }
        foreach (var group in requestLines.GroupBy(item => allocationById[item.SalesOrderAllocationId].StockLevelId))
        {
            var stock = stockById[group.Key].Stock;
            var quantity = group.Sum(item => item.Quantity);
            if (stock.QuantityAllocated < quantity || stock.QuantityOnHand < quantity)
                throw new ConflictException("Reserved physical stock is insufficient for this pickup.");
        }
        return stockById.ToDictionary(row => row.Key, row => row.Value.Stock);
    }

    public static SalesOrderPickup ExecutePickup(IWmsDbContext context,
        SalesOrder order, int tenantId, string actor, CreateSalesOrderPickupCommand request,
        ReservationData reservations, Dictionary<int, StockLevel> stockById)
    {
        var now = DateTime.UtcNow;
        var pickup = SalesOrderPickup.Create(tenantId, order.Id, actor, now, request.ReceivedBy, request.Notes);
        context.SalesOrderPickups.Add(pickup);
        foreach (var item in request.Lines)
        {
            var allocation = reservations.AllocationsById[item.SalesOrderAllocationId];
            var line = reservations.LinesByAllocation[allocation.Id];
            var stock = stockById[allocation.StockLevelId];
            var pickupLine = pickup.AddLine(allocation.Id, item.Quantity);
            stock.Ship(item.Quantity, actor, now);
            line.RecordPickup(allocation, item.Quantity);
            if (reservations.PickedByAllocation.GetValueOrDefault(allocation.Id) + item.Quantity == allocation.Quantity)
                allocation.MarkShipped();
            context.StockMovements.Add(StockMovement.CreatePickupShipment(
                tenantId, line.ProductId, stock.LocationId, item.Quantity,
                order.Id, pickupLine, actor, now));
        }
        order.UpdateFulfillmentStatus(actor, now);
        return pickup;
    }

    public sealed record ReservationData(
        Dictionary<long, SalesOrderAllocation> AllocationsById,
        Dictionary<long, SalesOrderLine> LinesByAllocation,
        Dictionary<long, decimal> PickedByAllocation);
}
