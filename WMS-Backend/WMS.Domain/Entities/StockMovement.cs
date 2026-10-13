using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities;

public sealed class StockMovement : IMustBelongToTenant
{
    public long Id { get; private set; }
    public int TenantId { get; set; }
    public int ProductId { get; private set; }
    public int LocationId { get; private set; }
    public decimal Quantity { get; private set; }
    public MovementType MovementType { get; private set; }
    public string? ReferenceTable { get; private set; }
    public int? ReferenceId { get; private set; }
    public long? SalesOrderPickupLineId { get; private set; }
    public SalesOrderPickupLine? SalesOrderPickupLine { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;

    private StockMovement() { }

    public static StockMovement Create(
        int tenantId,
        int productId,
        int locationId,
        decimal quantity,
        MovementType movementType,
        string createdBy,
        DateTime createdOnUtc,
        string? referenceTable = null,
        int? referenceId = null)
    {
        if (tenantId <= 0 || productId <= 0 || locationId <= 0)
            throw new ArgumentException("Tenant, product, and location IDs must be positive.");
        if (quantity == 0)
            throw new ArgumentException("Movement quantity cannot be zero.", nameof(quantity));
        if (movementType == MovementType.Receipt && quantity < 0)
            throw new ArgumentException("Receipt movement quantity must be positive.", nameof(quantity));
        if (movementType == MovementType.TransferOut && quantity > 0)
            throw new ArgumentException("Transfer-out movement quantity must be negative.", nameof(quantity));
        if (movementType == MovementType.TransferIn && quantity < 0)
            throw new ArgumentException("Transfer-in movement quantity must be positive.", nameof(quantity));
        if (string.IsNullOrWhiteSpace(createdBy) || createdBy.Length > 128)
            throw new ArgumentException("Creator is required and cannot exceed 128 characters.", nameof(createdBy));

        return new StockMovement
        {
            TenantId = tenantId,
            ProductId = productId,
            LocationId = locationId,
            Quantity = quantity,
            MovementType = movementType,
            ReferenceTable = referenceTable,
            ReferenceId = referenceId,
            CreatedBy = createdBy,
            CreatedOnUtc = createdOnUtc
        };
    }

    public static StockMovement CreatePickupShipment(int tenantId, int productId, int locationId,
        decimal quantity, int salesOrderId, SalesOrderPickupLine pickupLine,
        string createdBy, DateTime createdOnUtc)
    {
        ArgumentNullException.ThrowIfNull(pickupLine);
        if (quantity <= 0 || pickupLine.TenantId != tenantId || pickupLine.Quantity != quantity)
            throw new ArgumentException("Shipment must match a positive pickup line quantity.", nameof(quantity));
        if (salesOrderId <= 0)
            throw new ArgumentOutOfRangeException(nameof(salesOrderId));
        var movement = Create(tenantId, productId, locationId, -quantity,
            MovementType.Shipment, createdBy, createdOnUtc, "SalesOrders", salesOrderId);
        movement.SalesOrderPickupLine = pickupLine;
        return movement;
    }
}
