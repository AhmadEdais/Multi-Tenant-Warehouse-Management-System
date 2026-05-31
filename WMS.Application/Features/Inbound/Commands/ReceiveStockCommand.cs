namespace WMS.Application.Features.Inbound.Commands;

public record ReceiveStockCommand(
    int PurchaseOrderId,
    int ProductId,
    int LocationId,
    int Quantity) : IRequest<int>;
public class ReceiveStockCommandValidator : AbstractValidator<ReceiveStockCommand>
{
    public ReceiveStockCommandValidator()
    {
        RuleFor(x => x.PurchaseOrderId).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.LocationId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}   
internal class ReceiveStockCommandHandler(IWmsDbContext context,ITenantContext tenantContext) : IRequestHandler<ReceiveStockCommand, int>
{
    public async Task<int> Handle(ReceiveStockCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("Tenant context is not available.");

        var location = await context.Locations
            .FirstOrDefaultAsync(l => l.Id == request.LocationId, cancellationToken)
            ?? throw new NotFoundException("Location not found.");

        if (location.LocationFunction != LocationFunction.Dock)
        {
            throw new ValidationException("Stock can only be received directly into a Dock location.");
        }

        var purchaseOrder = await context.PurchaseOrders
            .Include(po => po.Lines)
            .FirstOrDefaultAsync(po => po.Id == request.PurchaseOrderId, cancellationToken)
            ?? throw new NotFoundException("Purchase Order not found.");

        var poLine = purchaseOrder.Lines.FirstOrDefault(p => p.ProductId == request.ProductId)
            ?? throw new ValidationException($"Product ID '{request.ProductId}' is not on this Purchase Order.");

        poLine.Receive(request.Quantity);
        purchaseOrder.TryMarkAsFullyReceived();

        var stockLevel = await context.StockLevels
            .FirstOrDefaultAsync(sl => sl.ProductId == request.ProductId && sl.LocationId == request.LocationId, cancellationToken);

        if (stockLevel == null)
        {
            stockLevel = StockLevel.Create(tenantId, request.ProductId, request.LocationId);
            stockLevel.Receive(request.Quantity); 
            context.StockLevels.Add(stockLevel);
        }
        else
        {
            stockLevel.Receive(request.Quantity);
        }

        var stockMovement = StockMovement.Create(
            tenantId,
            request.ProductId,
            request.LocationId,
            request.Quantity,
            MovementType.Receipt, 
            "PurchaseOrders",    
            purchaseOrder.Id);    

        context.StockMovements.Add(stockMovement);

        await context.SaveChangesAsync(cancellationToken);

        return purchaseOrder.Id;
    }
}
