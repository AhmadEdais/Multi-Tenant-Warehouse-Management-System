namespace WMS.Application.Features.Inbound.Commands;

public sealed record CreatePurchaseOrderLineDto(
    int ProductId,
    decimal ExpectedQuantity,
    decimal UnitCost);

public sealed record CreatePurchaseOrderCommand(
    int SupplierId,
    int WarehouseId,
    string OrderNumber,
    DateTime? ExpectedDeliveryDate,
    List<CreatePurchaseOrderLineDto> Lines) : IRequest<int>;
public class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0);
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.OrderNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ExpectedDeliveryDate)
            .GreaterThanOrEqualTo(DateTime.Today)
            .When(x => x.ExpectedDeliveryDate.HasValue);
        RuleFor(x => x.Lines)
            .NotEmpty()
            .Must(lines => lines.All(line => line.ProductId > 0 && line.ExpectedQuantity > 0 && line.UnitCost >= 0))
            .WithMessage("Each line must have a valid ProductId, ExpectedQuantity greater than 0, and nonnegative UnitCost.")
            .Must(lines => lines.Select(l => l.ProductId).Distinct().Count() == lines.Count)
            .WithMessage("A product can only appear once per Purchase Order."); ;
    }
}
internal sealed class CreatePurchaseOrderCommandHandler(
    IWmsDbContext context,
    ITenantContext tenantContext,
    ICurrentUserService currentUser) : IRequestHandler<CreatePurchaseOrderCommand, int>
{
    public async Task<int> Handle(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("Must be in a tenant context.");

        var warehouseExists = await context.Warehouses
            .AnyAsync(w => w.Id == request.WarehouseId && w.IsActive, cancellationToken);

        if (!warehouseExists)
        {
            throw new NotFoundException("Active warehouse was not found.");
        }

        var createdByUserId = currentUser.UserId
             ?? throw new UnauthorizedAccessException(
        "A signed-in user is required to create a Purchase Order.");

        var supplierExists = await context.Suppliers
            .AnyAsync(s => s.Id == request.SupplierId && s.IsActive, cancellationToken);
        if (!supplierExists)
        {
            throw new NotFoundException("The specified supplier does not exist.");
        }

        var orderNumberExists = await context.PurchaseOrders
            .AnyAsync(po => po.OrderNumber == request.OrderNumber, cancellationToken);

        if (orderNumberExists)
        {
            throw new ConflictException($"A Purchase Order with Order Number '{request.OrderNumber}' already exists.");
        }
        var productIds = request.Lines
            .Select(l => l.ProductId)
            .Distinct()
            .ToList();
        var validProductIds = await context.Products
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
        if (validProductIds.Count != productIds.Count)
        {
            throw new NotFoundException(
                "One or more products were not found.");
        }

        var purchaseOrder = PurchaseOrder.Create(
            tenantId,
            request.WarehouseId,
            request.SupplierId,
            request.OrderNumber,
            request.ExpectedDeliveryDate,
            createdByUserId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        foreach (var line in request.Lines)
        {
            purchaseOrder.AddLine(line.ProductId, line.ExpectedQuantity, line.UnitCost);
        }
        context.PurchaseOrders.Add(purchaseOrder);
        await context.SaveChangesAsync(cancellationToken);
        return purchaseOrder.Id;
    }
}
