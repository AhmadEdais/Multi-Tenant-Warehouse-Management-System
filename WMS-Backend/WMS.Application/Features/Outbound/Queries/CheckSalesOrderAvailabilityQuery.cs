namespace WMS.Application.Features.Outbound.Queries;

public sealed record CheckSalesOrderAvailabilityLineDto(int ProductId, decimal Quantity);

public sealed record CheckSalesOrderAvailabilityQuery(
    int WarehouseId,
    List<CheckSalesOrderAvailabilityLineDto> Lines) : IRequest<SalesOrderAvailabilityResultDto>;

public sealed record SalesOrderLineAvailabilityDto(
    int ProductId,
    decimal RequestedQuantity,
    decimal AvailableQuantity,
    decimal ShortageQuantity,
    bool HasSufficientStock);

public sealed record SalesOrderAvailabilityResultDto(
    int WarehouseId,
    bool AllLinesSufficient,
    List<SalesOrderLineAvailabilityDto> Lines);

public sealed class CheckSalesOrderAvailabilityQueryValidator : AbstractValidator<CheckSalesOrderAvailabilityQuery>
{
    public CheckSalesOrderAvailabilityQueryValidator()
    {
        RuleFor(query => query.WarehouseId).GreaterThan(0);
        RuleFor(query => query.Lines)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(lines => lines.All(line => line is not null && line.ProductId > 0
                && line.Quantity > 0 && line.Quantity < 10000000000000000m
                && decimal.Round(line.Quantity, 2) == line.Quantity))
            .WithMessage("Every line needs a valid ProductId and positive decimal(18,2) Quantity.")
            .Must(lines => lines.Select(line => line.ProductId).Distinct().Count() == lines.Count)
            .WithMessage("A product can only appear once in an availability check.");
    }
}

internal sealed class CheckSalesOrderAvailabilityQueryHandler(
    IWmsDbContext context,
    ITenantContext tenantContext) : IRequestHandler<CheckSalesOrderAvailabilityQuery, SalesOrderAvailabilityResultDto>
{
    public async Task<SalesOrderAvailabilityResultDto> Handle(
        CheckSalesOrderAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedAccessException("A tenant workspace is required to check Sales Order availability.");

        var warehouseExists = await context.Warehouses.AsNoTracking()
            .AnyAsync(warehouse => warehouse.TenantId == tenantId
                && warehouse.Id == request.WarehouseId && warehouse.IsActive, cancellationToken);
        if (!warehouseExists)
            throw new NotFoundException("Active warehouse was not found.");

        var productIds = request.Lines.Select(line => line.ProductId).ToArray();
        var activeProductCount = await context.Products.AsNoTracking()
            .CountAsync(product => product.TenantId == tenantId
                && productIds.Contains(product.Id) && product.IsActive, cancellationToken);
        if (activeProductCount != productIds.Length)
            throw new NotFoundException("One or more active products were not found.");

        var availableByProduct = await (
            from stock in context.StockLevels.AsNoTracking()
            join location in context.Locations.AsNoTracking()
                on stock.LocationId equals location.Id
            where stock.TenantId == tenantId
                && location.TenantId == tenantId
                && location.WarehouseId == request.WarehouseId
                && location.IsActive
                && location.LocationType == LocationTypes.Bin
                && productIds.Contains(stock.ProductId)
                && stock.QuantityOnHand > stock.QuantityAllocated
            group stock by stock.ProductId into productStock
            select new
            {
                ProductId = productStock.Key,
                AvailableQuantity = productStock.Sum(stock => stock.QuantityOnHand - stock.QuantityAllocated)
            }).ToDictionaryAsync(item => item.ProductId, item => item.AvailableQuantity, cancellationToken);

        var lines = request.Lines.Select(line =>
        {
            var available = availableByProduct.GetValueOrDefault(line.ProductId);
            var shortage = Math.Max(0m, line.Quantity - available);
            return new SalesOrderLineAvailabilityDto(
                line.ProductId, line.Quantity, available, shortage, shortage == 0);
        }).ToList();

        return new SalesOrderAvailabilityResultDto(
            request.WarehouseId, lines.All(line => line.HasSufficientStock), lines);
    }
}
