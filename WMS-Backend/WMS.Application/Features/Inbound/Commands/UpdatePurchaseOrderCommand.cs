using WMS.Domain.Enums;

namespace WMS.Application.Features.Inbound.Commands;

public sealed record UpdatePurchaseOrderLineDto(
    int ProductId,
    decimal ExpectedQuantity,
    decimal UnitCost);

public sealed record UpdatePurchaseOrderCommand(
    [property: JsonIgnore] int Id,
    int SupplierId,
    int WarehouseId,
    DateTime? ExpectedDeliveryDate,
    List<UpdatePurchaseOrderLineDto> Lines) : IRequest;

public sealed class UpdatePurchaseOrderCommandValidator : AbstractValidator<UpdatePurchaseOrderCommand>
{
    public UpdatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.SupplierId).GreaterThan(0);
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.ExpectedDeliveryDate)
            .GreaterThanOrEqualTo(DateTime.Today)
            .When(x => x.ExpectedDeliveryDate.HasValue);
        RuleFor(x => x.Lines)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(lines => lines.All(line => line is not null && line.ProductId > 0 && line.ExpectedQuantity > 0 && line.UnitCost >= 0))
            .WithMessage("Each line must have a valid ProductId, ExpectedQuantity greater than 0, and nonnegative UnitCost.")
            .Must(lines => lines.Select(line => line.ProductId).Distinct().Count() == lines.Count)
            .WithMessage("A product can only appear once per Purchase Order.");
    }
}

internal sealed class UpdatePurchaseOrderCommandHandler(
    IWmsDbContext context,
    ITenantContext tenantContext,
    ICurrentUserService currentUser) : IRequestHandler<UpdatePurchaseOrderCommand>
{
    public async Task Handle(UpdatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to update a Purchase Order.");

        var modifiedByUserId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("A signed-in user is required to update a Purchase Order.");

        var purchaseOrder = await context.PurchaseOrders
            .Include(po => po.Lines)
            .FirstOrDefaultAsync(po => po.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Purchase Order was not found.");

        if (purchaseOrder.Status != PurchaseOrderStatus.Draft)
            throw new ConflictException("Only a draft Purchase Order can be edited.");

        var supplierExists = await context.Suppliers
            .AnyAsync(s => s.Id == request.SupplierId && s.IsActive, cancellationToken);
        if (!supplierExists)
            throw new NotFoundException("Active supplier was not found.");

        var warehouseExists = await context.Warehouses
            .AnyAsync(w => w.Id == request.WarehouseId && w.IsActive, cancellationToken);
        if (!warehouseExists)
            throw new NotFoundException("Active warehouse was not found.");

        var productIds = request.Lines.Select(line => line.ProductId).Distinct().ToList();
        var validProductCount = await context.Products
            .CountAsync(p => productIds.Contains(p.Id) && p.IsActive, cancellationToken);
        if (validProductCount != productIds.Count)
            throw new NotFoundException("One or more active products were not found.");

        purchaseOrder.ReplaceDraftLines(request.Lines.Select(line =>
            (line.ProductId, line.ExpectedQuantity, line.UnitCost)));
        purchaseOrder.UpdateDraftDetails(
            request.SupplierId,
            request.WarehouseId,
            request.ExpectedDeliveryDate,
            modifiedByUserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            DateTime.UtcNow);

        await context.SaveChangesAsync(cancellationToken);
    }
}
