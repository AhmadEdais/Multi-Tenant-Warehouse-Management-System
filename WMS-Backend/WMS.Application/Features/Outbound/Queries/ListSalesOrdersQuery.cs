using WMS.Domain.Enums;

namespace WMS.Application.Features.Outbound.Queries;

public sealed record SalesOrderListDto(
    int Id,
    string OrderNumber,
    string Status,
    int CustomerId,
    string CustomerCode,
    string CustomerName,
    int WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    DateTime? RequestedShipmentDate,
    DateTime CreatedOnUtc,
    decimal TotalAmount,
    decimal TotalOrderedQuantity,
    decimal TotalAllocatedQuantity,
    decimal TotalPickedQuantity,
    decimal TotalRemainingQuantity);

public sealed record ListSalesOrdersQuery(
    string? Search = null,
    string? Status = null,
    int? CustomerId = null,
    int? WarehouseId = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<SalesOrderListDto>>;

public sealed class ListSalesOrdersQueryValidator : AbstractValidator<ListSalesOrdersQuery>
{
    public ListSalesOrdersQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x.CustomerId).GreaterThan(0).When(x => x.CustomerId.HasValue);
        RuleFor(x => x.WarehouseId).GreaterThan(0).When(x => x.WarehouseId.HasValue);
        RuleFor(x => x.Status)
            .Must(status => status is null || Enum.GetNames<SalesOrderStatus>()
                .Contains(status.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("Status must be a valid Sales Order status.");
    }
}

internal sealed class ListSalesOrdersQueryHandler(IWmsDbContext context, ITenantContext tenantContext)
    : IRequestHandler<ListSalesOrdersQuery, PagedResult<SalesOrderListDto>>
{
    public async Task<PagedResult<SalesOrderListDto>> Handle(
        ListSalesOrdersQuery request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to view Sales Orders.");

        var query =
            from order in context.SalesOrders.AsNoTracking()
            join customer in context.Customers.AsNoTracking()
                on order.CustomerId equals customer.Id
            join warehouse in context.Warehouses.AsNoTracking()
                on order.WarehouseId equals warehouse.Id
            select new { Order = order, Customer = customer, Warehouse = warehouse };

        var search = request.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Order.OrderNumber.Contains(search)
                || x.Customer.Code.Contains(search)
                || x.Customer.Name.Contains(search)
                || x.Warehouse.Code.Contains(search)
                || x.Warehouse.Name.Contains(search));

        if (request.Status is not null)
        {
            var status = Enum.Parse<SalesOrderStatus>(request.Status.Trim(), true);
            query = query.Where(x => x.Order.Status == status);
        }
        if (request.CustomerId is int customerId)
            query = query.Where(x => x.Order.CustomerId == customerId);
        if (request.WarehouseId is int warehouseId)
            query = query.Where(x => x.Order.WarehouseId == warehouseId);

        var totalCount = await query.CountAsync(cancellationToken);
        var page = await query
            .OrderByDescending(x => x.Order.CreatedOnUtc)
            .ThenByDescending(x => x.Order.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new
            {
                x.Order.Id,
                x.Order.OrderNumber,
                x.Order.Status,
                x.Order.CustomerId,
                CustomerCode = x.Customer.Code,
                CustomerName = x.Customer.Name,
                x.Order.WarehouseId,
                WarehouseCode = x.Warehouse.Code,
                WarehouseName = x.Warehouse.Name,
                x.Order.RequestedShipmentDate,
                x.Order.CreatedOnUtc,
                TotalAmount = context.SalesOrderLines.AsNoTracking()
                    .Where(line => line.SalesOrderId == x.Order.Id)
                    .Sum(line => (decimal?)(line.Quantity * line.UnitPrice)) ?? 0m,
                TotalOrderedQuantity = context.SalesOrderLines.AsNoTracking()
                    .Where(line => line.SalesOrderId == x.Order.Id)
                    .Sum(line => (decimal?)line.Quantity) ?? 0m,
                TotalAllocatedQuantity = context.SalesOrderLines.AsNoTracking()
                    .Where(line => line.SalesOrderId == x.Order.Id)
                    .Sum(line => (decimal?)line.QuantityAllocated) ?? 0m,
                TotalPickedQuantity = (
                    from pickupLine in context.SalesOrderPickupLines.AsNoTracking()
                    join allocation in context.SalesOrderAllocations.AsNoTracking()
                        on pickupLine.SalesOrderAllocationId equals allocation.Id
                    join orderLine in context.SalesOrderLines.AsNoTracking()
                        on allocation.SalesOrderLineId equals orderLine.Id
                    where orderLine.SalesOrderId == x.Order.Id
                    select (decimal?)pickupLine.Quantity).Sum() ?? 0m
            })
            .ToListAsync(cancellationToken);

        var items = page.Select(x => new SalesOrderListDto(
            x.Id, x.OrderNumber, x.Status.ToString(), x.CustomerId,
            x.CustomerCode, x.CustomerName, x.WarehouseId, x.WarehouseCode,
            x.WarehouseName, x.RequestedShipmentDate, x.CreatedOnUtc,
            x.TotalAmount, x.TotalOrderedQuantity, x.TotalAllocatedQuantity,
            x.TotalPickedQuantity, x.TotalOrderedQuantity - x.TotalPickedQuantity)).ToList();

        return new PagedResult<SalesOrderListDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
