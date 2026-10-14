namespace WMS.Application.Features.Outbound.Commands;

public sealed record CreateSalesOrderLineDto(int ProductId, decimal Quantity, decimal UnitPrice);

public sealed record CreateSalesOrderCommand(
    int CustomerId,
    int WarehouseId,
    DateTime? RequestedShipmentDate,
    List<CreateSalesOrderLineDto> Lines) : IRequest<int>;

public sealed class CreateSalesOrderCommandValidator : AbstractValidator<CreateSalesOrderCommand>
{
    public CreateSalesOrderCommandValidator()
    {
        RuleFor(command => command.CustomerId).GreaterThan(0);
        RuleFor(command => command.WarehouseId).GreaterThan(0);
        RuleFor(command => command.RequestedShipmentDate)
            .GreaterThanOrEqualTo(DateTime.Today)
            .When(command => command.RequestedShipmentDate.HasValue);
        RuleFor(command => command.Lines)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(lines => lines.All(line => line is not null && line.ProductId > 0
                && line.Quantity > 0 && line.Quantity < 10000000000000000m
                && decimal.Round(line.Quantity, 2) == line.Quantity
                && line.UnitPrice >= 0 && line.UnitPrice < 100000000000000m
                && decimal.Round(line.UnitPrice, 4) == line.UnitPrice))
            .WithMessage("Every line needs a valid ProductId, positive decimal(18,2) Quantity, and nonnegative decimal(18,4) UnitPrice.")
            .Must(lines => lines.Select(line => line.ProductId).Distinct().Count() == lines.Count)
            .WithMessage("A product can only appear once on a Sales Order.");
    }
}

internal sealed class CreateSalesOrderCommandHandler(
    IWmsDbContext context,
    ITenantContext tenantContext,
    ICurrentUserService currentUser,
    ISalesOrderNumberGenerator numberGenerator) : IRequestHandler<CreateSalesOrderCommand, int>
{
    public async Task<int> Handle(CreateSalesOrderCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedAccessException("A tenant workspace is required to create a Sales Order.");
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("A signed-in user is required to create a Sales Order.");
        var customerExists = await context.Customers
            .AnyAsync(customer => customer.Id == request.CustomerId && customer.IsActive, cancellationToken);
        if (!customerExists)
            throw new NotFoundException("Active customer was not found.");
        var warehouseExists = await context.Warehouses.AnyAsync(warehouse =>
                warehouse.Id == request.WarehouseId && warehouse.IsActive, cancellationToken);
        if (!warehouseExists)
            throw new NotFoundException("Active warehouse was not found.");

        var productIds = request.Lines.Select(line => line.ProductId).Distinct().ToList();
        var activeProductCount = await context.Products.CountAsync(product =>
            productIds.Contains(product.Id) && product.IsActive, cancellationToken);
        if (activeProductCount != productIds.Count)
            throw new NotFoundException("One or more active products were not found.");

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var orderNumber = await numberGenerator.ReserveNextAsync(cancellationToken);
        var order = SalesOrder.Create(tenantId, request.CustomerId, request.WarehouseId,
            orderNumber, request.RequestedShipmentDate,
            userId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        foreach (var line in request.Lines)
            order.AddLine(line.ProductId, line.Quantity, line.UnitPrice);

        context.SalesOrders.Add(order);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return order.Id;
    }
}
