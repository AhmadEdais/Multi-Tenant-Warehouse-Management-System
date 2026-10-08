using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities;

public sealed class PurchaseOrderSequence : IMustBelongToTenant
{
    public int TenantId { get; set; }
    public long LastNumber { get; private set; }

    private PurchaseOrderSequence() { }

    public static PurchaseOrderSequence Create(int tenantId, long lastNumber)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantId));

        if (lastNumber < 0)
            throw new ArgumentOutOfRangeException(nameof(lastNumber));

        return new PurchaseOrderSequence { TenantId = tenantId, LastNumber = lastNumber };
    }

    public long ReserveNext()
    {
        LastNumber = checked(LastNumber + 1);
        return LastNumber;
    }
}
