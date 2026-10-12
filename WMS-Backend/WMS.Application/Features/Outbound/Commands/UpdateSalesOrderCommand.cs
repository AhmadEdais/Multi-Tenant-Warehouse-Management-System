using WMS.Domain.Enums;

namespace WMS.Application.Features.Outbound.Commands;

public sealed record UpdateSalesOrderLineDto(int ProductId, decimal Quantity, decimal UnitPrice);

public sealed record UpdateSalesOrderCommand(
    [property: JsonIgnore] int Id,
    int CustomerId,
    int WarehouseId,
    DateTime? RequestedShipmentDate,
    List<UpdateSalesOrderLineDto> Lines) : IRequest;

public sealed class UpdateSalesOrderCommandValidator : AbstractValidator<UpdateSalesOrderCommand>
{
    public UpdateSalesOrderCommandValidator()
    {
        RuleFor(command => command.Id).GreaterThan(0);
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

internal sealed class UpdateSalesOrderCommandHandler(
    IWmsDbContext context,
    ITenantContext tenantContext,
    ICurrentUserService currentUser) : IRequestHandler<UpdateSalesOrderCommand>
{
    public async Task Handle(UpdateSalesOrderCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to update a Sales Order.");
        var modifiedByUserId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("A signed-in user is required to update a Sales Order.");

        var order = await context.SalesOrders
            .Include(item => item.Lines)
            .ThenInclude(line => line.Allocations)
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Sales Order was not found.");

        if (order.Status != SalesOrderStatus.Draft)
            throw new ConflictException("Only a draft Sales Order can be edited.");
        if (order.Lines.Any(line => line.QuantityAllocated != 0 || line.Allocations.Count != 0))
            throw new ConflictException("A draft Sales Order with reservations cannot be edited.");

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

        order.ReplaceDraftLines(request.Lines.Select(line =>
            (line.ProductId, line.Quantity, line.UnitPrice)));
        order.UpdateDraftDetails(request.CustomerId, request.WarehouseId,
            request.RequestedShipmentDate,
            modifiedByUserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            DateTime.UtcNow);

        await context.SaveChangesAsync(cancellationToken);
    }
}
