using WMS.Domain.Enums;

namespace WMS.Application.Features.Outbound.Queries;

public sealed record SalesOrderAllocationDetailsDto(
    long Id,
    int StockLevelId,
    int LocationId,
    string LocationName,
    string? LocationBarcode,
    decimal Quantity,
    string Status,
    string CreatedBy,
    DateTime CreatedOnUtc,
    string? ReleasedBy,
    DateTime? ReleasedOnUtc,
    string? ReleaseReason);

public sealed record SalesOrderLineDetailsDto(
    long Id,
    int ProductId,
    string ProductSku,
    string ProductName,
    decimal Quantity,
    decimal QuantityAllocated,
    decimal RemainingQuantity,
    decimal UnitPrice,
    decimal LineTotal,
    List<SalesOrderAllocationDetailsDto> Allocations);

public sealed record SalesOrderDetailsDto(
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
    string CreatedBy,
    string CreatedByFullName,
    DateTime? LastModifiedOnUtc,
    string? LastModifiedBy,
    string? LastModifiedByFullName,
    string RowVersion,
    decimal TotalAmount,
    List<SalesOrderLineDetailsDto> Lines);

public sealed record GetSalesOrderByIdQuery(int Id) : IRequest<SalesOrderDetailsDto>;

public sealed class GetSalesOrderByIdQueryValidator : AbstractValidator<GetSalesOrderByIdQuery>
{
    public GetSalesOrderByIdQueryValidator() => RuleFor(x => x.Id).GreaterThan(0);
}

internal sealed class GetSalesOrderByIdQueryHandler(IWmsDbContext context, ITenantContext tenantContext)
    : IRequestHandler<GetSalesOrderByIdQuery, SalesOrderDetailsDto>
{
    public async Task<SalesOrderDetailsDto> Handle(
        GetSalesOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedAccessException("A tenant workspace is required to view Sales Orders.");

        var header = await (
            from order in context.SalesOrders.AsNoTracking()
            where order.Id == request.Id
            join customer in context.Customers.AsNoTracking()
                on order.CustomerId equals customer.Id
            join warehouse in context.Warehouses.AsNoTracking()
                on order.WarehouseId equals warehouse.Id
            select new
            {
                order.Id,
                order.OrderNumber,
                order.Status,
                order.CustomerId,
                CustomerCode = customer.Code,
                CustomerName = customer.Name,
                order.WarehouseId,
                WarehouseCode = warehouse.Code,
                WarehouseName = warehouse.Name,
                order.RequestedShipmentDate,
                order.CreatedOnUtc,
                order.CreatedBy,
                order.LastModifiedOnUtc,
                order.LastModifiedBy,
                order.RowVersion,
                TotalAmount = context.SalesOrderLines.AsNoTracking()
                    .Where(line => line.SalesOrderId == order.Id)
                    .Sum(line => (decimal?)(line.Quantity * line.UnitPrice)) ?? 0m
            }).FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Sales Order was not found.");

        var lineRows = await (
            from line in context.SalesOrderLines.AsNoTracking()
            where line.SalesOrderId == header.Id
            join product in context.Products.AsNoTracking()
                on line.ProductId equals product.Id
            orderby line.Id
            select new
            {
                line.Id,
                line.ProductId,
                ProductSku = product.SKU,
                ProductName = product.Name,
                line.Quantity,
                line.QuantityAllocated,
                RemainingQuantity = line.Quantity - line.QuantityAllocated,
                line.UnitPrice,
                LineTotal = line.Quantity * line.UnitPrice
            }).ToListAsync(cancellationToken);

        var allocationRows = header.Status == SalesOrderStatus.Draft
            ? []
            : await (
                from allocation in context.SalesOrderAllocations.AsNoTracking()
                join line in context.SalesOrderLines.AsNoTracking()
                    on allocation.SalesOrderLineId equals line.Id
                where line.SalesOrderId == header.Id
                join stock in context.StockLevels.AsNoTracking()
                    on allocation.StockLevelId equals stock.Id
                join location in context.Locations.AsNoTracking()
                    on stock.LocationId equals location.Id
                orderby location.Id, allocation.Id
                select new
                {
                    allocation.SalesOrderLineId,
                    allocation.Id,
                    allocation.StockLevelId,
                    stock.LocationId,
                    LocationName = location.Name,
                    LocationBarcode = location.Barcode,
                    allocation.Quantity,
                    allocation.Status,
                    allocation.CreatedBy,
                    allocation.CreatedOnUtc,
                    allocation.ReleasedBy,
                    allocation.ReleasedOnUtc,
                    allocation.ReleaseReason
                }).ToListAsync(cancellationToken);

        var allocationsByLine = allocationRows
            .GroupBy(allocation => allocation.SalesOrderLineId)
            .ToDictionary(group => group.Key, group => group.Select(allocation =>
                new SalesOrderAllocationDetailsDto(
                    allocation.Id, allocation.StockLevelId, allocation.LocationId,
                    allocation.LocationName, allocation.LocationBarcode,
                    allocation.Quantity, allocation.Status.ToString(),
                    allocation.CreatedBy, allocation.CreatedOnUtc,
                    allocation.ReleasedBy, allocation.ReleasedOnUtc,
                    allocation.ReleaseReason)).ToList());

        var lines = lineRows.Select(line => new SalesOrderLineDetailsDto(
            line.Id, line.ProductId, line.ProductSku, line.ProductName,
            line.Quantity, line.QuantityAllocated, line.RemainingQuantity,
            line.UnitPrice, line.LineTotal,
            allocationsByLine.GetValueOrDefault(line.Id) ?? [])).ToList();

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

        return new SalesOrderDetailsDto(
            header.Id, header.OrderNumber, header.Status.ToString(),
            header.CustomerId, header.CustomerCode, header.CustomerName,
            header.WarehouseId, header.WarehouseCode, header.WarehouseName,
            header.RequestedShipmentDate, header.CreatedOnUtc,
            header.CreatedBy, ResolveActorName(header.CreatedBy) ?? header.CreatedBy,
            header.LastModifiedOnUtc, header.LastModifiedBy,
            ResolveActorName(header.LastModifiedBy),
            Convert.ToBase64String(header.RowVersion), header.TotalAmount, lines);
    }
}
