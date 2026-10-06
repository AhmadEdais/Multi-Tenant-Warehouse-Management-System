using WMS.Domain.Enums;

namespace WMS.Application.Features.Inbound.Commands;

public sealed record CancelPurchaseOrderCommand(int Id) : IRequest;

public sealed class CancelPurchaseOrderCommandValidator : AbstractValidator<CancelPurchaseOrderCommand>
{
    public CancelPurchaseOrderCommandValidator() => RuleFor(x => x.Id).GreaterThan(0);
}

internal sealed class CancelPurchaseOrderCommandHandler(
    IWmsDbContext context,
    ITenantContext tenantContext,
    ICurrentUserService currentUser) : IRequestHandler<CancelPurchaseOrderCommand>
{
    public async Task Handle(CancelPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to cancel a Purchase Order.");

        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("A signed-in user is required to cancel a Purchase Order.");

        var po = await context.PurchaseOrders
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Purchase Order was not found.");

        if (po.Status is not (PurchaseOrderStatus.Draft or PurchaseOrderStatus.Pending))
            throw new ConflictException("Only a draft or pending Purchase Order can be canceled.");

        po.Cancel();
        po.Touch(userId.ToString(System.Globalization.CultureInfo.InvariantCulture), DateTime.UtcNow);
        await context.SaveChangesAsync(cancellationToken);
    }
}
