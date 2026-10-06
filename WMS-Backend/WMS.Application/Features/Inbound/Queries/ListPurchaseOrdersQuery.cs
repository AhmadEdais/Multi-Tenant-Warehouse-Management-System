using WMS.Domain.Enums;

namespace WMS.Application.Features.Inbound.Queries;

public sealed record PurchaseOrderListDto(
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
    decimal TotalAmount,
    decimal TotalExpectedQuantity,
    decimal TotalReceivedQuantity);

public sealed record ListPurchaseOrdersQuery(
    string? Search = null,
    string? Status = null,
    int? SupplierId = null,
    int? WarehouseId = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<PurchaseOrderListDto>>;

public sealed class ListPurchaseOrdersQueryValidator : AbstractValidator<ListPurchaseOrdersQuery>
{
    public ListPurchaseOrdersQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x.SupplierId).GreaterThan(0).When(x => x.SupplierId.HasValue);
        RuleFor(x => x.WarehouseId).GreaterThan(0).When(x => x.WarehouseId.HasValue);
        RuleFor(x => x.Status)
            .Must(status => status is null || Enum.GetNames<PurchaseOrderStatus>()
                .Contains(status.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("Status must be a valid Purchase Order status.");
    }
}

internal sealed class ListPurchaseOrdersQueryHandler(IWmsDbContext context, ITenantContext tenantContext)
    : IRequestHandler<ListPurchaseOrdersQuery, PagedResult<PurchaseOrderListDto>>
{
    public async Task<PagedResult<PurchaseOrderListDto>> Handle(
        ListPurchaseOrdersQuery request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to view Purchase Orders.");

        var query =
            from po in context.PurchaseOrders.AsNoTracking()
            join supplier in context.Suppliers.AsNoTracking() 
            on po.SupplierId equals supplier.Id
            join warehouse in context.Warehouses.AsNoTracking()
            on po.WarehouseId equals warehouse.Id
            select new { Po = po, Supplier = supplier, Warehouse = warehouse };

        var search = request.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Po.OrderNumber.Contains(search)
                || x.Supplier.Code.Contains(search)
                || x.Supplier.Name.Contains(search)
                || x.Warehouse.Code.Contains(search)
                || x.Warehouse.Name.Contains(search));

        if (request.Status is not null)
        {
            var status = Enum.Parse<PurchaseOrderStatus>(request.Status.Trim(), true);
            query = query.Where(x => x.Po.Status == status);
        }
        if (request.SupplierId is int supplierId)
            query = query.Where(x => x.Po.SupplierId == supplierId);
        if (request.WarehouseId is int warehouseId)
            query = query.Where(x => x.Po.WarehouseId == warehouseId);

        var totalCount = await query.CountAsync(cancellationToken);
        var page = await query
            .OrderByDescending(x => x.Po.CreatedOnUtc)
            .ThenByDescending(x => x.Po.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new
            {
                x.Po.Id,
                x.Po.OrderNumber,
                x.Po.Status,
                x.Po.SupplierId,
                SupplierCode = x.Supplier.Code,
                SupplierName = x.Supplier.Name,
                x.Po.WarehouseId,
                WarehouseCode = x.Warehouse.Code,
                WarehouseName = x.Warehouse.Name,
                x.Po.ExpectedDeliveryDate,
                x.Po.CreatedOnUtc,
                TotalAmount = context.PurchaseOrderLines.AsNoTracking()
                    .Where(line => line.PurchaseOrderId == x.Po.Id)
                    .Sum(line => (decimal?)(line.ExpectedQuantity * line.UnitCost)) ?? 0m,
                TotalExpectedQuantity = context.PurchaseOrderLines.AsNoTracking()
                    .Where(line => line.PurchaseOrderId == x.Po.Id)
                    .Sum(line => (decimal?)line.ExpectedQuantity) ?? 0m,
                TotalReceivedQuantity = context.PurchaseOrderLines.AsNoTracking()
                    .Where(line => line.PurchaseOrderId == x.Po.Id)
                    .Sum(line => (decimal?)line.ReceivedQuantity) ?? 0m
            })
            .ToListAsync(cancellationToken);

        var items = page.Select(x => new PurchaseOrderListDto(
            x.Id, x.OrderNumber, x.Status.ToString(), x.SupplierId,
            x.SupplierCode, x.SupplierName, x.WarehouseId, x.WarehouseCode,
            x.WarehouseName, x.ExpectedDeliveryDate, x.CreatedOnUtc,
            x.TotalAmount, x.TotalExpectedQuantity, x.TotalReceivedQuantity)).ToList();

        return new PagedResult<PurchaseOrderListDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
