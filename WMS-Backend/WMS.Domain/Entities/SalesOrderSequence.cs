using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities;

public sealed class SalesOrderSequence : IMustBelongToTenant
{
    public int TenantId { get; set; }
    public long LastNumber { get; private set; }

    private SalesOrderSequence() { }

    public static SalesOrderSequence Create(int tenantId, long lastNumber)
    {
        if (tenantId <= 0) throw new ArgumentOutOfRangeException(nameof(tenantId));
        if (lastNumber < 0) throw new ArgumentOutOfRangeException(nameof(lastNumber));
        return new SalesOrderSequence { TenantId = tenantId, LastNumber = lastNumber };
    }

    public long ReserveNext() => LastNumber = checked(LastNumber + 1);
}
