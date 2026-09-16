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

    private PurchaseOrderLine() { }

    internal static PurchaseOrderLine Create(int tenantId, int productId, decimal expectedQuantity)
    {
        if (expectedQuantity <= 0)
            throw new ArgumentException("Expected quantity must be greater than zero.");

        return new PurchaseOrderLine
        {
            TenantId = tenantId,
            ProductId = productId,
            ExpectedQuantity = expectedQuantity,
            ReceivedQuantity = 0
        };
    }

    public void Receive(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Receive quantity must be positive.");

        if (ReceivedQuantity + quantity > ExpectedQuantity)
            throw new InvalidOperationException($"Cannot receive {quantity}. It exceeds the expected remaining amount.");

        ReceivedQuantity += quantity;
    }
}