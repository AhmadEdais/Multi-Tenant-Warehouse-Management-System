namespace WMS.Application.Features.Inbound.Commands;

public sealed record CreatePurchaseOrderLineDto(
    int ProductId,
    decimal ExpectedQuantity); 

public sealed record CreatePurchaseOrderCommand(
    int SupplierId,
    string OrderNumber,            
    DateTime? ExpectedDeliveryDate,
    List<CreatePurchaseOrderLineDto> Lines) : IRequest<int>;
public class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0);
        RuleFor(x => x.OrderNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ExpectedDeliveryDate)
            .GreaterThanOrEqualTo(DateTime.Today)
            .When(x => x.ExpectedDeliveryDate.HasValue);
        RuleFor(x => x.Lines)
            .NotEmpty()
            .Must(lines => lines.All(line => line.ProductId > 0 && line.ExpectedQuantity > 0))
            .WithMessage("Each line must have a valid ProductId and ExpectedQuantity greater than 0.")
            .Must(lines => lines.Select(l => l.ProductId).Distinct().Count() == lines.Count)
            .WithMessage("A product can only appear once per Purchase Order."); ;
    }
}
internal sealed class CreatePurchaseOrderCommandHandler(IWmsDbContext context, ITenantContext tenantContext) : IRequestHandler<CreatePurchaseOrderCommand, int>
{
    public async Task<int> Handle(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("Must be in a tenant context.");
        var supplierExists = await context.Suppliers
            .AnyAsync(s => s.Id == request.SupplierId, cancellationToken);
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
        var purchaseOrder = PurchaseOrder.Create(
            tenantId,
            request.SupplierId,
            request.OrderNumber,
            request.ExpectedDeliveryDate);
        foreach (var line in request.Lines)
        {
            purchaseOrder.AddLine(line.ProductId, line.ExpectedQuantity);
        }
        context.PurchaseOrders.Add(purchaseOrder);
        await context.SaveChangesAsync(cancellationToken);
        return purchaseOrder.Id;
    }
}