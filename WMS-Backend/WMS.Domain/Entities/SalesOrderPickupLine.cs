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

    internal static SalesOrderPickupLine Create(int tenantId, long salesOrderAllocationId, decimal quantity)
    {
        if (tenantId <= 0 || salesOrderAllocationId <= 0)
            throw new ArgumentException("Tenant and allocation IDs must be positive.");
        if (quantity <= 0 || quantity >= 10000000000000000m || decimal.Round(quantity, 2) != quantity)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Pickup quantity must fit decimal(18,2) and be positive.");
        return new SalesOrderPickupLine
        {
            TenantId = tenantId, SalesOrderAllocationId = salesOrderAllocationId, Quantity = quantity
        };
    }
}
