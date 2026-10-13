using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities;

public sealed class SalesOrderPickupLine : IMustBelongToTenant
{
    public long Id { get; private set; }
    public int TenantId { get; set; }
    public long SalesOrderPickupId { get; private set; }
    public long SalesOrderAllocationId { get; private set; }
    public decimal Quantity { get; private set; }

    private SalesOrderPickupLine() { }
}
