using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities;

public sealed class SalesOrderLine : IMustBelongToTenant
{
    public long Id { get; private set; }
    public int TenantId { get; set; }
    public int SalesOrderId { get; private set; }
    public int ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal QuantityAllocated { get; private set; }
    public decimal UnitPrice { get; private set; }

    private readonly List<SalesOrderAllocation> _allocations = [];
    public IReadOnlyCollection<SalesOrderAllocation> Allocations => _allocations.AsReadOnly();

    private SalesOrderLine() { }

    internal static SalesOrderLine Create(int tenantId, int productId, decimal quantity, decimal unitPrice)
    {
        if (tenantId <= 0 || productId <= 0)
            throw new ArgumentException("Tenant and product IDs must be positive.");
        ValidateDraftValues(quantity, unitPrice);

        return new SalesOrderLine
        {
            TenantId = tenantId,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }

    internal void UpdateDraftValues(decimal quantity, decimal unitPrice)
    {
        if (QuantityAllocated != 0 || _allocations.Count != 0)
            throw new InvalidOperationException("A Sales Order line with reservations cannot be edited as a draft.");

        ValidateDraftValues(quantity, unitPrice);
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    private static void ValidateDraftValues(decimal quantity, decimal unitPrice)
    {
        if (quantity <= 0 || quantity >= 10000000000000000m || decimal.Round(quantity, 2) != quantity)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must fit decimal(18,2) and be positive.");
        if (unitPrice < 0 || unitPrice >= 100000000000000m || decimal.Round(unitPrice, 4) != unitPrice)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price must fit decimal(18,4) and be nonnegative.");
    }

    public void Allocate(StockLevel stock, decimal quantity, string actor, DateTime allocatedOnUtc)
    {
        if (stock.TenantId != TenantId || stock.ProductId != ProductId)
            throw new InvalidOperationException("The stock does not belong to this Sales Order line.");
        if (quantity <= 0 || quantity > Quantity - QuantityAllocated)
            throw new InvalidOperationException("Allocation exceeds the quantity remaining on this Sales Order line.");
        if (_allocations.Any(allocation => allocation.StockLevelId == stock.Id))
            throw new InvalidOperationException("The same stock level cannot be allocated twice to a Sales Order line.");

        stock.Reserve(quantity, actor, allocatedOnUtc);
        QuantityAllocated += quantity;
        _allocations.Add(SalesOrderAllocation.Create(TenantId, stock.Id, quantity, actor, allocatedOnUtc));
    }
}
