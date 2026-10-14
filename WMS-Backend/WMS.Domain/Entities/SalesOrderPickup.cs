using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities;

public sealed class SalesOrderPickup : IMustBelongToTenant
{
    public long Id { get; private set; }
    public int TenantId { get; set; }
    public int SalesOrderId { get; private set; }
    public DateTime PickedOnUtc { get; private set; }
    public string PickedBy { get; private set; } = string.Empty;
    public string? ReceivedBy { get; private set; }
    public string? Notes { get; private set; }

    private readonly List<SalesOrderPickupLine> _lines = [];
    public IReadOnlyCollection<SalesOrderPickupLine> Lines => _lines.AsReadOnly();

    private SalesOrderPickup() { }

    public static SalesOrderPickup Create(int tenantId, int salesOrderId, string pickedBy,
        DateTime pickedOnUtc, string? receivedBy, string? notes)
    {
        if (tenantId <= 0 || salesOrderId <= 0)
            throw new ArgumentException("Tenant and Sales Order IDs must be positive.");
        if (string.IsNullOrWhiteSpace(pickedBy) || pickedBy.Length > 128)
            throw new ArgumentException("Actor is required and cannot exceed 128 characters.", nameof(pickedBy));
        receivedBy = string.IsNullOrWhiteSpace(receivedBy) ? null : receivedBy.Trim();
        notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (receivedBy?.Length > 200 || notes?.Length > 500)
            throw new ArgumentException("Pickup notes or recipient exceed the allowed length.");

        return new SalesOrderPickup
        {
            TenantId = tenantId, SalesOrderId = salesOrderId, PickedBy = pickedBy,
            PickedOnUtc = pickedOnUtc, ReceivedBy = receivedBy, Notes = notes
        };
    }

    public SalesOrderPickupLine AddLine(long salesOrderAllocationId, decimal quantity)
    {
        if (_lines.Any(line => line.SalesOrderAllocationId == salesOrderAllocationId))
            throw new InvalidOperationException("An allocation can appear only once in a pickup.");
        var line = SalesOrderPickupLine.Create(TenantId, salesOrderAllocationId, quantity);
        _lines.Add(line);
        return line;
    }
}
