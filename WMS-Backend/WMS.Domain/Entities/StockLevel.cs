using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities;

public sealed class StockLevel : IMustBelongToTenant
{
    public int Id { get; private set; }
    public int TenantId { get;  set; }
    public int ProductId { get; private set; }
    public int LocationId { get; private set; }

    public decimal QuantityOnHand { get; private set; }
    public decimal QuantityAllocated { get; private set; }

    public byte[] RowVersion { get; private set; } = [];
    public DateTime CreatedOnUtc { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }

    public decimal AvailableQuantity => QuantityOnHand - QuantityAllocated;

    private StockLevel() { }

    public static StockLevel Create(int tenantId, int productId, int locationId, string createdBy, DateTime createdOnUtc)
    {
        if (tenantId <= 0 || productId <= 0 || locationId <= 0)
            throw new ArgumentException("Tenant, product, and location IDs must be positive.");
        if (string.IsNullOrWhiteSpace(createdBy) || createdBy.Length > 128)
            throw new ArgumentException("Creator is required and cannot exceed 128 characters.", nameof(createdBy));

        return new StockLevel
        {
            TenantId = tenantId,
            ProductId = productId,
            LocationId = locationId,
            QuantityOnHand = 0,
            QuantityAllocated = 0,
            CreatedBy = createdBy,
            CreatedOnUtc = createdOnUtc
        };
    }

    
    public void Receive(decimal quantity, string modifiedBy, DateTime modifiedOnUtc)
    {
        if (quantity <= 0)
            throw new ArgumentException("Receive quantity must be positive.", nameof(quantity));
        if (string.IsNullOrWhiteSpace(modifiedBy) || modifiedBy.Length > 128)
            throw new ArgumentException("Modifier is required and cannot exceed 128 characters.", nameof(modifiedBy));

        QuantityOnHand += quantity;
        LastModifiedBy = modifiedBy;
        LastModifiedOnUtc = modifiedOnUtc;
    }

    
    public void Reserve(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Reserve quantity must be positive.", nameof(quantity));

        if (AvailableQuantity < quantity)
            throw new InvalidOperationException($"Cannot reserve {quantity}. Only {AvailableQuantity} available.");

        QuantityAllocated += quantity;
    }

    public void Ship(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Shipment quantity must be positive.", nameof(quantity));

        if (QuantityAllocated < quantity)
            throw new InvalidOperationException("Cannot ship more than is currently allocated.");

        QuantityOnHand -= quantity;
        QuantityAllocated -= quantity;
    }

    public void Adjust(decimal deltaQuantity)
    {
        // deltaQuantity can be positive or negative
        if (QuantityOnHand + deltaQuantity < QuantityAllocated)
            throw new InvalidOperationException("Adjustment would drop physical inventory below currently allocated amount.");

        QuantityOnHand += deltaQuantity;
    }
}
