using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities;

public sealed class SalesOrderAllocation : IMustBelongToTenant
{
    public long Id { get; private set; }
    public int TenantId { get; set; }
    public long SalesOrderLineId { get; private set; }
    public int StockLevelId { get; private set; }
    public decimal Quantity { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;

    private SalesOrderAllocation() { }

    internal static SalesOrderAllocation Create(int tenantId, int stockLevelId, decimal quantity,
        string createdBy, DateTime createdOnUtc)
    {
        if (tenantId <= 0 || stockLevelId <= 0)
            throw new ArgumentException("Tenant and stock level IDs must be positive.");
        if (quantity <= 0 || quantity >= 10000000000000000m || decimal.Round(quantity, 2) != quantity)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        if (string.IsNullOrWhiteSpace(createdBy) || createdBy.Length > 128)
            throw new ArgumentException("Creator is required and cannot exceed 128 characters.", nameof(createdBy));

        return new SalesOrderAllocation
        {
            TenantId = tenantId,
            StockLevelId = stockLevelId,
            Quantity = quantity,
            CreatedBy = createdBy,
            CreatedOnUtc = createdOnUtc
        };
    }
}
