using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities;

public sealed class PurchaseOrder : IMustBelongToTenant
{
    public int Id { get; private set; }
    public int TenantId { get; set; }
    public int SupplierId { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public PurchaseOrderStatus Status { get; private set; }
    public DateTime? ExpectedDeliveryDate { get; private set; }

    private readonly List<PurchaseOrderLine> _lines = [];
    public IReadOnlyCollection<PurchaseOrderLine> Lines => _lines.AsReadOnly();

    private PurchaseOrder() { }

    public static PurchaseOrder Create(int tenantId, int supplierId, string orderNumber, DateTime? expectedDeliveryDate)
    {
        return new PurchaseOrder
        {
            TenantId = tenantId,
            SupplierId = supplierId,
            OrderNumber = orderNumber,
            Status = PurchaseOrderStatus.Pending,
            ExpectedDeliveryDate = expectedDeliveryDate
        };
    }

    public void AddLine(int productId, decimal expectedQuantity)
    {
        if (Status != PurchaseOrderStatus.Pending)
            throw new InvalidOperationException("Cannot add lines to an order that is already being processed.");

        if (_lines.Any(l => l.ProductId == productId))
            throw new InvalidOperationException($"Product {productId} is already on this Purchase Order.");

        _lines.Add(PurchaseOrderLine.Create(TenantId, productId, expectedQuantity));
    }

    public void MarkAsReceiving()
    {
        if (Status == PurchaseOrderStatus.Pending)
        {
            Status = PurchaseOrderStatus.Receiving;
        }
    }

    public void TryMarkAsFullyReceived()
    {
        // If all lines have their full quantity, close the PO
        if (_lines.All(l => l.ReceivedQuantity >= l.ExpectedQuantity))
        {
            Status = PurchaseOrderStatus.Received;
        }
    }
}