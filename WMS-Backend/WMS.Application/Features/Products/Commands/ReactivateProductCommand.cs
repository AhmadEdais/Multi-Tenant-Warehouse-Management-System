namespace WMS.Application.Features.Products.Commands;

public record ReactivateProductCommand(int Id) : IRequest;

public sealed class ReactivateProductCommandValidator : AbstractValidator<ReactivateProductCommand>
{
    public ReactivateProductCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

internal sealed class ReactivateProductCommandHandler(IWmsDbContext context, ITenantContext tenantContext)
    : IRequestHandler<ReactivateProductCommand>
{
    public async Task Handle(ReactivateProductCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to reactivate products.");

        var product = await context.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");
        if (product.IsActive)
            throw new ConflictException("Product is already active.");

        product.Reactivate();
        await context.SaveChangesAsync(cancellationToken);
    }
}
