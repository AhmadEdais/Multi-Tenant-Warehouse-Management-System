using WMS.Domain.Enums;
using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities;

public sealed class StockMovement : IMustBelongToTenant
{
    public int Id { get; private set; } 
    public int TenantId { get; set; }
    public int ProductId { get; private set; }
    public int LocationId { get; private set; }
    public decimal Quantity { get; private set; }
    public MovementType MovementType { get; private set; }
    public string? ReferenceTable { get; private set; }
    public int? ReferenceId { get; private set; }

    private StockMovement() { }

    public static StockMovement Create(
        int tenantId,
        int productId,
        int locationId,
        decimal quantity,
        MovementType movementType,
        string? referenceTable = null,
        int? referenceId = null)
    {
        return new StockMovement
        {
            TenantId = tenantId,
            ProductId = productId,
            LocationId = locationId,
            Quantity = quantity,
            MovementType = movementType,
            ReferenceTable = referenceTable,
            ReferenceId = referenceId
        };
    }
}