namespace WMS.Application.Features.Products.Commands;

public record DeactivateProductCommand(int Id) : IRequest;

public sealed class DeactivateProductCommandValidator : AbstractValidator<DeactivateProductCommand>
{
    public DeactivateProductCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

internal sealed class DeactivateProductCommandHandler(IWmsDbContext context, ITenantContext tenantContext)
    : IRequestHandler<DeactivateProductCommand>
{
    public async Task Handle(DeactivateProductCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to deactivate products.");

        var product = await context.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");
        if (!product.IsActive)
            throw new ConflictException("Product is already inactive.");

        product.Deactivate();
        await context.SaveChangesAsync(cancellationToken);
    }
}
