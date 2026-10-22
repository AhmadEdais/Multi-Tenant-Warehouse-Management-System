using WMS.Application.Common.Operations;
using WMS.Domain.Enums;

namespace WMS.Application.Features.Outbound.Commands;

public sealed record ApproveSalesOrderCommand(int Id) : IRequest;

public sealed class ApproveSalesOrderCommandValidator : AbstractValidator<ApproveSalesOrderCommand>
{
    public ApproveSalesOrderCommandValidator() => RuleFor(command => command.Id).GreaterThan(0);
}

internal sealed class ApproveSalesOrderCommandHandler(
    IWmsDbContext context,
    ITenantContext tenantContext,
    ICurrentUserService currentUser) : IRequestHandler<ApproveSalesOrderCommand>
{
    public async Task Handle(ApproveSalesOrderCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to approve a Sales Order.");
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("A signed-in user is required to approve a Sales Order.");
        var actor = userId.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var order = await context.SalesOrders.Include(item => item.Lines)
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Sales Order was not found.");
        if (order.Status != SalesOrderStatus.Draft)
            throw new ConflictException("Only a draft Sales Order can be approved.");
        if (order.Lines.Count == 0 || order.Lines.Any(line => line.QuantityAllocated != 0))
            throw new ConflictException("A draft Sales Order must contain unallocated lines before approval.");

        if (!await context.Customers.AnyAsync(customer =>
                customer.Id == order.CustomerId && customer.IsActive, cancellationToken))
            throw new ConflictException("The Sales Order customer is no longer active or available.");
        if (!await context.Warehouses.AnyAsync(warehouse =>
                warehouse.Id == order.WarehouseId && warehouse.IsActive, cancellationToken))
            throw new ConflictException("The Sales Order warehouse is no longer active or available.");

        var productIds = order.Lines.Select(line => line.ProductId).Distinct().ToArray();
        var activeProductCount = await context.Products.CountAsync(product =>
            productIds.Contains(product.Id) && product.IsActive, cancellationToken);
        if (activeProductCount != productIds.Length)
            throw new ConflictException("One or more Sales Order products are no longer active or available.");

        var binIds = await context.Locations.AsNoTracking()
            .Where(location => location.WarehouseId == order.WarehouseId
                && location.IsActive && location.LocationType == LocationTypes.Bin)
            .Select(location => location.Id)
            .ToListAsync(cancellationToken);
        if (binIds.Count == 0)
            throw new ConflictException("The warehouse has no active Bins available for allocation.");

        var stockLevels = await context.StockLevels
            .Where(stock => productIds.Contains(stock.ProductId)
                && binIds.Contains(stock.LocationId)
                && stock.QuantityOnHand > stock.QuantityAllocated)
            .ToListAsync(cancellationToken);
        SalesOrderAllocationOperations.Allocate(order, stockLevels, actor);
        await context.SaveChangesAsync(cancellationToken);
    }
}
