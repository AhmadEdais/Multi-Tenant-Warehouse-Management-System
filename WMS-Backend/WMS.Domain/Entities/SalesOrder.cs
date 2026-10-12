using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities;

public sealed class SalesOrder : IMustBelongToTenant
{
    public int Id { get; private set; }
    public int TenantId { get; set; }
    public int CustomerId { get; private set; }
    public int WarehouseId { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public SalesOrderStatus Status { get; private set; }
    public DateTime? RequestedShipmentDate { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public DateTime CreatedOnUtc { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }

    private readonly List<SalesOrderLine> _lines = [];
    public IReadOnlyCollection<SalesOrderLine> Lines => _lines.AsReadOnly();

    private SalesOrder() { }

    public static SalesOrder Create(int tenantId, int customerId, int warehouseId,
        string orderNumber, DateTime? requestedShipmentDate, string createdBy)
    {
        if (tenantId <= 0 || customerId <= 0 || warehouseId <= 0)
            throw new ArgumentException("Tenant, customer, and warehouse IDs must be positive.");
        if (string.IsNullOrWhiteSpace(orderNumber) || orderNumber.Length > 50)
            throw new ArgumentException("Order number is required and cannot exceed 50 characters.", nameof(orderNumber));
        if (string.IsNullOrWhiteSpace(createdBy) || createdBy.Length > 128)
            throw new ArgumentException("Creator is required and cannot exceed 128 characters.", nameof(createdBy));

        return new SalesOrder
        {
            TenantId = tenantId,
            CustomerId = customerId,
            WarehouseId = warehouseId,
            OrderNumber = orderNumber,
            Status = SalesOrderStatus.Draft,
            RequestedShipmentDate = requestedShipmentDate,
            CreatedBy = createdBy
        };
    }

    public void AddLine(int productId, decimal quantity, decimal unitPrice)
    {
        if (Status != SalesOrderStatus.Draft)
            throw new InvalidOperationException("Lines can only be added to a draft Sales Order.");
        if (_lines.Any(line => line.ProductId == productId))
            throw new InvalidOperationException("A product can only appear once on a Sales Order.");
        _lines.Add(SalesOrderLine.Create(TenantId, productId, quantity, unitPrice));
    }

    public void MarkAllocated(string modifiedBy, DateTime modifiedOnUtc)
    {
        if (Status != SalesOrderStatus.Draft)
            throw new InvalidOperationException("Only a draft Sales Order can be allocated.");
        if (_lines.Count == 0 || _lines.Any(line => line.QuantityAllocated != line.Quantity))
            throw new InvalidOperationException("Every Sales Order line must be fully allocated before approval.");
        if (string.IsNullOrWhiteSpace(modifiedBy) || modifiedBy.Length > 128)
            throw new ArgumentException("Modifier is required and cannot exceed 128 characters.", nameof(modifiedBy));

        Status = SalesOrderStatus.Allocated;
        LastModifiedBy = modifiedBy;
        LastModifiedOnUtc = modifiedOnUtc;
    }
}
