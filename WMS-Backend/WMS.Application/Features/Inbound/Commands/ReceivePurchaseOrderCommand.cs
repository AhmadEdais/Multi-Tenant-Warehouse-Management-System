using WMS.Domain.Enums;

namespace WMS.Application.Features.Inbound.Commands;

public sealed record ReceivePurchaseOrderLineDto(long PurchaseOrderLineId, decimal Quantity);

public sealed record ReceivePurchaseOrderCommand(
    [property: JsonIgnore] int Id,
    List<ReceivePurchaseOrderLineDto> Lines) : IRequest;

public sealed class ReceivePurchaseOrderCommandValidator : AbstractValidator<ReceivePurchaseOrderCommand>
{
    public ReceivePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Lines)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(lines => lines.All(line => line is not null
                && line.PurchaseOrderLineId > 0 && line.Quantity > 0))
            .WithMessage("Each receipt line must have a valid PurchaseOrderLineId and positive Quantity.")
            .Must(lines => lines.All(line => line.Quantity == decimal.Round(line.Quantity, 2)
                && line.Quantity < 10000000000000000m))
            .WithMessage("Receipt quantities must fit decimal(18,2).")
            .Must(lines => lines.Select(line => line.PurchaseOrderLineId).Distinct().Count() == lines.Count)
            .WithMessage("A Purchase Order line can only appear once in a receipt.");
    }
}

internal sealed class ReceivePurchaseOrderCommandHandler(
    IWmsDbContext context,
    ITenantContext tenantContext,
    ICurrentUserService currentUser) : IRequestHandler<ReceivePurchaseOrderCommand>
{
    public async Task Handle(ReceivePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedAccessException("A tenant workspace is required to receive a Purchase Order.");
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("A signed-in user is required to receive a Purchase Order.");
        var actor = userId.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var po = await context.PurchaseOrders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Purchase Order was not found.");

        if (po.Status is not (PurchaseOrderStatus.Pending or PurchaseOrderStatus.Receiving))
            throw new ConflictException("Only a pending or receiving Purchase Order can receive stock.");

        var docks = await context.Locations
            .Where(x => x.WarehouseId == po.WarehouseId && x.LocationType == LocationTypes.Dock)
            .ToListAsync(cancellationToken);
        if (docks.Count != 1 || !docks[0].IsActive || docks[0].ParentLocationId.HasValue)
            throw new ConflictException("The Purchase Order warehouse must have exactly one active Receiving Dock.");
        var dock = docks[0];

        var poLines = po.Lines.ToDictionary(x => x.Id);
        foreach (var receipt in request.Lines)
        {
            if (!poLines.TryGetValue(receipt.PurchaseOrderLineId, out var line))
                throw new ConflictException("A receipt line does not belong to this Purchase Order.");
            if (receipt.Quantity > line.ExpectedQuantity - line.ReceivedQuantity)
                throw new ConflictException("A receipt quantity exceeds the remaining expected quantity.");
        }

        var productIds = request.Lines.Select(x => poLines[x.PurchaseOrderLineId].ProductId).ToList();
        var stockLevels = await context.StockLevels
            .Where(x => x.LocationId == dock.Id && productIds.Contains(x.ProductId))
            .ToListAsync(cancellationToken);
        var stockByProduct = stockLevels.ToDictionary(x => x.ProductId);
        var now = DateTime.UtcNow;

        foreach (var receipt in request.Lines)
        {
            var line = poLines[receipt.PurchaseOrderLineId];
            line.Receive(receipt.Quantity);

            if (!stockByProduct.TryGetValue(line.ProductId, out var stock))
            {
                stock = StockLevel.Create(tenantId, line.ProductId, dock.Id, actor, now);
                context.StockLevels.Add(stock);
                stockByProduct.Add(line.ProductId, stock);
            }
            stock.Receive(receipt.Quantity, actor, now);

            context.StockMovements.Add(StockMovement.Create(
                tenantId, line.ProductId, dock.Id, receipt.Quantity,
                MovementType.Receipt, actor, now, "PurchaseOrders", po.Id));
        }

        po.MarkAsReceiving();
        po.TryMarkAsFullyReceived();
        po.Touch(actor, now);
        await context.SaveChangesAsync(cancellationToken);
    }
}
