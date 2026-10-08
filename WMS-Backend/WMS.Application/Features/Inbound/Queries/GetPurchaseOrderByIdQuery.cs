namespace WMS.Application.Features.Inbound.Queries;

public sealed record PurchaseOrderLineDetailsDto(
    long Id,
    int ProductId,
    string ProductSku,
    string ProductName,
    decimal ExpectedQuantity,
    decimal ReceivedQuantity,
    decimal RemainingQuantity,
    decimal UnitCost,
    decimal LineTotal);

public sealed record PurchaseOrderDetailsDto(
    int Id,
    string OrderNumber,
    string Status,
    int SupplierId,
    string SupplierCode,
    string SupplierName,
    int WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    DateTime? ExpectedDeliveryDate,
    DateTime CreatedOnUtc,
    string CreatedBy,
    string CreatedByFullName,
    DateTime? LastModifiedOnUtc,
    string? LastModifiedBy,
    string? LastModifiedByFullName,
    string RowVersion,
    decimal TotalAmount,
    List<PurchaseOrderLineDetailsDto> Lines);

public sealed record GetPurchaseOrderByIdQuery(int Id) : IRequest<PurchaseOrderDetailsDto>;

public sealed class GetPurchaseOrderByIdQueryValidator : AbstractValidator<GetPurchaseOrderByIdQuery>
{
    public GetPurchaseOrderByIdQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

internal sealed class GetPurchaseOrderByIdQueryHandler(IWmsDbContext context, ITenantContext tenantContext)
    : IRequestHandler<GetPurchaseOrderByIdQuery, PurchaseOrderDetailsDto>
{
    public async Task<PurchaseOrderDetailsDto> Handle(
        GetPurchaseOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedAccessException("A tenant workspace is required to view Purchase Orders.");

        var header = await (
            from purchaseOrder in context.PurchaseOrders.AsNoTracking()
            where purchaseOrder.Id == request.Id
            join supplier in context.Suppliers.AsNoTracking()
                on purchaseOrder.SupplierId equals supplier.Id
            join warehouse in context.Warehouses.AsNoTracking()
                on purchaseOrder.WarehouseId equals warehouse.Id
            select new
            {
                purchaseOrder.Id,
                purchaseOrder.OrderNumber,
                purchaseOrder.Status,
                purchaseOrder.SupplierId,
                SupplierCode = supplier.Code,
                SupplierName = supplier.Name,
                purchaseOrder.WarehouseId,
                WarehouseCode = warehouse.Code,
                WarehouseName = warehouse.Name,
                purchaseOrder.ExpectedDeliveryDate,
                purchaseOrder.CreatedOnUtc,
                purchaseOrder.CreatedBy,
                purchaseOrder.LastModifiedOnUtc,
                purchaseOrder.LastModifiedBy,
                purchaseOrder.RowVersion,
                TotalAmount = context.PurchaseOrderLines.AsNoTracking()
                    .Where(line => line.PurchaseOrderId == purchaseOrder.Id)
                    .Sum(line => (decimal?)(line.ExpectedQuantity * line.UnitCost)) ?? 0m
            }).FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Purchase Order was not found.");

        var lines = await (
            from line in context.PurchaseOrderLines.AsNoTracking()
            where line.PurchaseOrderId == header.Id
            join product in context.Products.AsNoTracking()
                on line.ProductId equals product.Id
            orderby line.Id
            select new PurchaseOrderLineDetailsDto(
                line.Id,
                line.ProductId,
                product.SKU,
                product.Name,
                line.ExpectedQuantity,
                line.ReceivedQuantity,
                line.ExpectedQuantity - line.ReceivedQuantity,
                line.UnitCost,
                line.ExpectedQuantity * line.UnitCost))
            .ToListAsync(cancellationToken);

        var actorIds = new[] { header.CreatedBy, header.LastModifiedBy }
            .Select(value => int.TryParse(value, System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        var actorNames = actorIds.Length == 0
            ? new Dictionary<int, string>()
            : await context.Users.AsNoTracking()
                .Where(user => user.TenantId == tenantId && actorIds.Contains(user.Id))
                .Select(user => new { user.Id, user.FullName })
                .ToDictionaryAsync(user => user.Id, user => user.FullName, cancellationToken);

        string? ResolveActorName(string? storedActorId)
        {
            if (string.IsNullOrWhiteSpace(storedActorId)) return storedActorId;
            return int.TryParse(storedActorId, System.Globalization.NumberStyles.None,
                       System.Globalization.CultureInfo.InvariantCulture, out var id)
                   && actorNames.TryGetValue(id, out var fullName)
                   && !string.IsNullOrWhiteSpace(fullName)
                ? fullName
                : storedActorId;
        }

        return new PurchaseOrderDetailsDto(
            header.Id,
            header.OrderNumber,
            header.Status.ToString(),
            header.SupplierId,
            header.SupplierCode,
            header.SupplierName,
            header.WarehouseId,
            header.WarehouseCode,
            header.WarehouseName,
            header.ExpectedDeliveryDate,
            header.CreatedOnUtc,
            header.CreatedBy,
            ResolveActorName(header.CreatedBy) ?? header.CreatedBy,
            header.LastModifiedOnUtc,
            header.LastModifiedBy,
            ResolveActorName(header.LastModifiedBy),
            Convert.ToBase64String(header.RowVersion),
            header.TotalAmount,
            lines);
    }
}
