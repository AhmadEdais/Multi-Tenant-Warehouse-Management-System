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
}
