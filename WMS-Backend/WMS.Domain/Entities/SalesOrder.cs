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

    public void ReplaceDraftLines(IEnumerable<(int ProductId, decimal Quantity, decimal UnitPrice)> lines)
    {
        if (Status != SalesOrderStatus.Draft)
            throw new InvalidOperationException("Lines can only be changed on a draft Sales Order.");

        ArgumentNullException.ThrowIfNull(lines);
        if (_lines.Any(line => line.QuantityAllocated != 0 || line.Allocations.Count != 0))
            throw new InvalidOperationException("A Sales Order with reservations cannot be edited as a draft.");

        var replacements = lines
            .Select(line => SalesOrderLine.Create(TenantId, line.ProductId, line.Quantity, line.UnitPrice))
            .ToList();
        if (replacements.Count == 0)
            throw new InvalidOperationException("A Sales Order must contain at least one line.");

        var productIds = replacements.Select(line => line.ProductId).ToHashSet();
        if (productIds.Count != replacements.Count)
            throw new InvalidOperationException("A product can only appear once on a Sales Order.");

        var existingLines = _lines.ToDictionary(line => line.ProductId);
        _lines.RemoveAll(line => !productIds.Contains(line.ProductId));

        foreach (var replacement in replacements)
        {
            if (existingLines.TryGetValue(replacement.ProductId, out var existingLine))
                existingLine.UpdateDraftValues(replacement.Quantity, replacement.UnitPrice);
            else
                _lines.Add(replacement);
        }
    }

    public void UpdateDraftDetails(int customerId, int warehouseId,
        DateTime? requestedShipmentDate, string modifiedBy, DateTime modifiedOnUtc)
    {
        if (Status != SalesOrderStatus.Draft)
            throw new InvalidOperationException("Only a draft Sales Order can be edited.");
        if (_lines.Count == 0)
            throw new InvalidOperationException("A Sales Order must contain at least one line.");
        if (_lines.Any(line => line.QuantityAllocated != 0 || line.Allocations.Count != 0))
            throw new InvalidOperationException("A Sales Order with reservations cannot be edited as a draft.");
        if (customerId <= 0 || warehouseId <= 0)
            throw new ArgumentException("Customer and warehouse IDs must be positive.");
        if (string.IsNullOrWhiteSpace(modifiedBy) || modifiedBy.Length > 128)
            throw new ArgumentException("Modifier is required and cannot exceed 128 characters.", nameof(modifiedBy));

        CustomerId = customerId;
        WarehouseId = warehouseId;
        RequestedShipmentDate = requestedShipmentDate;
        LastModifiedBy = modifiedBy;
        LastModifiedOnUtc = LastModifiedOnUtc.HasValue && modifiedOnUtc <= LastModifiedOnUtc.Value
            ? LastModifiedOnUtc.Value.AddTicks(1)
            : modifiedOnUtc;
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

    public void Cancel(string modifiedBy, DateTime modifiedOnUtc)
    {
        if (Status is not (SalesOrderStatus.Draft or SalesOrderStatus.Allocated))
            throw new InvalidOperationException("Only a draft or allocated Sales Order can be canceled.");
        if (Status == SalesOrderStatus.Draft && _lines.Any(line =>
                line.QuantityAllocated != 0 || line.Allocations.Count != 0))
            throw new InvalidOperationException("A draft Sales Order with reservations cannot be canceled.");
        if (Status == SalesOrderStatus.Allocated && (_lines.Count == 0 || _lines.Any(line =>
                line.QuantityAllocated != 0 || line.Allocations.Any(allocation =>
                    allocation.Status == SalesOrderAllocationStatus.Active))))
            throw new InvalidOperationException("All Sales Order reservations must be released before cancellation.");
        if (string.IsNullOrWhiteSpace(modifiedBy) || modifiedBy.Length > 128)
            throw new ArgumentException("Modifier is required and cannot exceed 128 characters.", nameof(modifiedBy));

        Status = SalesOrderStatus.Canceled;
        LastModifiedBy = modifiedBy;
        LastModifiedOnUtc = modifiedOnUtc;
    }
}
