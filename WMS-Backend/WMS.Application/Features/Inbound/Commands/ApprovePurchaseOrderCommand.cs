using WMS.Domain.Enums;

namespace WMS.Application.Features.Inbound.Commands;

public sealed record ApprovePurchaseOrderCommand(int Id) : IRequest;

public sealed class ApprovePurchaseOrderCommandValidator : AbstractValidator<ApprovePurchaseOrderCommand>
{
    public ApprovePurchaseOrderCommandValidator() => RuleFor(x => x.Id).GreaterThan(0);
}

internal sealed class ApprovePurchaseOrderCommandHandler(
    IWmsDbContext context,
    ITenantContext tenantContext,
    ICurrentUserService currentUser) : IRequestHandler<ApprovePurchaseOrderCommand>
{
    public async Task Handle(ApprovePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to approve a Purchase Order.");

        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("A signed-in user is required to approve a Purchase Order.");

        var po = await context.PurchaseOrders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Purchase Order was not found.");

        if (po.Status != PurchaseOrderStatus.Draft)
            throw new ConflictException("Only a draft Purchase Order can be approved.");
        if (po.Lines.Count == 0)
            throw new ConflictException("A Purchase Order must contain at least one line before approval.");

        if (!await context.Suppliers.AnyAsync(x => x.Id == po.SupplierId && x.IsActive, cancellationToken))
            throw new ConflictException("The Purchase Order supplier is no longer active or available.");
        if (!await context.Warehouses.AnyAsync(x => x.Id == po.WarehouseId && x.IsActive, cancellationToken))
            throw new ConflictException("The Purchase Order warehouse is no longer active or available.");

        var productIds = po.Lines.Select(x => x.ProductId).Distinct().ToList();
        var activeProductCount = await context.Products
            .CountAsync(x => productIds.Contains(x.Id) && x.IsActive, cancellationToken);
        if (activeProductCount != productIds.Count)
            throw new ConflictException("One or more Purchase Order products are no longer active or available.");

        po.MarkAsPending();
        po.Touch(userId.ToString(System.Globalization.CultureInfo.InvariantCulture), DateTime.UtcNow);
        await context.SaveChangesAsync(cancellationToken);
    }
}
