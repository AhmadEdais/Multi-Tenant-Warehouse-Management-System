using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities;

public sealed class PurchaseOrder : IMustBelongToTenant
{
    public int Id { get; private set; }
    public int TenantId { get; set; }
    public int WarehouseId { get; private set; }
    public int SupplierId { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public PurchaseOrderStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public DateTime? ExpectedDeliveryDate { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }

    private readonly List<PurchaseOrderLine> _lines = [];
    public IReadOnlyCollection<PurchaseOrderLine> Lines => _lines.AsReadOnly();

    private PurchaseOrder() { }

    public static PurchaseOrder Create(
        int tenantId,
        int warehouseId,
        int supplierId,
        string orderNumber,
        DateTime? expectedDeliveryDate,
        string createdBy)
    {
        if (tenantId <= 0 || warehouseId <= 0 || supplierId <= 0)
            throw new ArgumentException("Tenant, warehouse, and supplier IDs must be positive.");

        if (string.IsNullOrWhiteSpace(orderNumber) || orderNumber.Length > 50)
            throw new ArgumentException("Order number is required and cannot exceed 50 characters.", nameof(orderNumber));

        if (string.IsNullOrWhiteSpace(createdBy) || createdBy.Length > 128)
            throw new ArgumentException("Creator is required and cannot exceed 128 characters.", nameof(createdBy));

        return new PurchaseOrder
        {
            TenantId = tenantId,
            WarehouseId = warehouseId,
            SupplierId = supplierId,
            OrderNumber = orderNumber,
            Status = PurchaseOrderStatus.Draft,
            ExpectedDeliveryDate = expectedDeliveryDate,
            CreatedBy = createdBy
        };
    }

    public void AddLine(int productId, decimal expectedQuantity, decimal unitCost)
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Lines can only be added to a draft Purchase Order.");

        if (_lines.Any(l => l.ProductId == productId))
            throw new InvalidOperationException($"Product {productId} is already on this Purchase Order.");

        _lines.Add(PurchaseOrderLine.Create(TenantId, productId, expectedQuantity, unitCost));
        LastModifiedOnUtc = DateTime.UtcNow;

    }

    public void ReplaceDraftLines(IEnumerable<(int ProductId, decimal ExpectedQuantity, decimal UnitCost)> lines)
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Lines can only be changed on a draft Purchase Order.");

        ArgumentNullException.ThrowIfNull(lines);

        var replacements = lines
            .Select(line => PurchaseOrderLine.Create(TenantId, line.ProductId, line.ExpectedQuantity, line.UnitCost))
            .ToList();

        if (replacements.Count == 0)
            throw new InvalidOperationException("A Purchase Order must contain at least one line.");

        var productIds = replacements.Select(line => line.ProductId).ToHashSet();
        if (productIds.Count != replacements.Count)
            throw new InvalidOperationException("A product can only appear once on a Purchase Order.");

        var existingLines = _lines.ToDictionary(line => line.ProductId);
        _lines.RemoveAll(line => !productIds.Contains(line.ProductId));

        foreach (var replacement in replacements)
        {
            if (existingLines.TryGetValue(replacement.ProductId, out var existingLine))
                existingLine.UpdateDraftValues(replacement.ExpectedQuantity, replacement.UnitCost);
            else
                _lines.Add(replacement);
        }
    }

    public void UpdateDraftDetails(
        int supplierId,
        int warehouseId,
        DateTime? expectedDeliveryDate,
        string modifiedBy,
        DateTime modifiedOnUtc)
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Only a draft Purchase Order can be edited.");

        if (_lines.Count == 0)
            throw new InvalidOperationException("A Purchase Order must contain at least one line.");

        if (supplierId <= 0 || warehouseId <= 0)
            throw new ArgumentException("Supplier and warehouse IDs must be positive.");

        if (string.IsNullOrWhiteSpace(modifiedBy) || modifiedBy.Length > 128)
            throw new ArgumentException("Modifier is required and cannot exceed 128 characters.", nameof(modifiedBy));

        SupplierId = supplierId;
        WarehouseId = warehouseId;
        ExpectedDeliveryDate = expectedDeliveryDate;
        LastModifiedOnUtc = modifiedOnUtc;
        LastModifiedBy = modifiedBy;
    }

    public void MarkAsPending()
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Only a draft Purchase Order can be submitted for approval.");

        if (_lines.Count == 0)
            throw new InvalidOperationException("A Purchase Order must have at least one line before approval.");

        Status = PurchaseOrderStatus.Pending;
        LastModifiedOnUtc = DateTime.UtcNow;

    }

    public void MarkAsReceiving()
    {
        if (Status == PurchaseOrderStatus.Receiving)
            return;

        if (Status != PurchaseOrderStatus.Pending)
            throw new InvalidOperationException("Only a pending Purchase Order can begin receiving.");

        Status = PurchaseOrderStatus.Receiving;
        LastModifiedOnUtc = DateTime.UtcNow;

    }

    public void TryMarkAsFullyReceived()
    {
        if (Status != PurchaseOrderStatus.Receiving)
            throw new InvalidOperationException("Only a receiving Purchase Order can be marked as received.");

        if (_lines.Count > 0 && _lines.All(l => l.ReceivedQuantity == l.ExpectedQuantity))
            Status = PurchaseOrderStatus.Received;
        LastModifiedOnUtc = DateTime.UtcNow;

    }

    public void Cancel()
    {
        if (Status is not (PurchaseOrderStatus.Draft or PurchaseOrderStatus.Pending))
            throw new InvalidOperationException("Only a draft or pending Purchase Order can be canceled.");

        Status = PurchaseOrderStatus.Canceled;
        LastModifiedOnUtc = DateTime.UtcNow;
    }

    public void Touch(string modifiedBy, DateTime modifiedOnUtc)
    {
        if (string.IsNullOrWhiteSpace(modifiedBy) || modifiedBy.Length > 128)
            throw new ArgumentException("Modifier is required and cannot exceed 128 characters.", nameof(modifiedBy));

        LastModifiedBy = modifiedBy;
        LastModifiedOnUtc = LastModifiedOnUtc.HasValue && modifiedOnUtc <= LastModifiedOnUtc.Value
            ? LastModifiedOnUtc.Value.AddTicks(1)
            : modifiedOnUtc;
    }
}
