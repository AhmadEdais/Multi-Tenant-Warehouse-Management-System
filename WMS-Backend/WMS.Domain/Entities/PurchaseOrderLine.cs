using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities;

public sealed class PurchaseOrderLine : IMustBelongToTenant
{
    public long Id { get; private set; }
    public int TenantId { get; set; }
    public int PurchaseOrderId { get; private set; }
    public int ProductId { get; private set; }

    public decimal ExpectedQuantity { get; private set; }
    public decimal ReceivedQuantity { get; private set; }
    public decimal UnitCost { get; private set; }

    private PurchaseOrderLine() { }

    internal static PurchaseOrderLine Create(int tenantId, int productId, decimal expectedQuantity, decimal unitCost)
    {
        if (tenantId <= 0 || productId <= 0)
            throw new ArgumentException("Tenant and product IDs must be positive.");

        if (expectedQuantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(expectedQuantity), "Expected quantity must be greater than zero.");

        if (unitCost < 0)
            throw new ArgumentOutOfRangeException(nameof(unitCost), "Unit cost cannot be negative.");

        return new PurchaseOrderLine
        {
            TenantId = tenantId,
            ProductId = productId,
            ExpectedQuantity = expectedQuantity,
            ReceivedQuantity = 0,
            UnitCost = unitCost
        };
    }

    public void Receive(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Receive quantity must be positive.");

        if (quantity > ExpectedQuantity - ReceivedQuantity)
            throw new InvalidOperationException($"Cannot receive {quantity}. It exceeds the expected remaining amount.");

        ReceivedQuantity += quantity;
    }
}
