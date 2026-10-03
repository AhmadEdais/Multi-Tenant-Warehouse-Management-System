namespace WMS.Application.Features.Products.Commands;

public record UpdateProductCommand(
    [property: JsonIgnore] int Id,
    string Name,
    string? Description,
    string UnitOfMeasure,
    decimal UnitCost,
    decimal UnitPrice,
    int ReorderPoint) : IRequest;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.UnitOfMeasure).NotEmpty().MaximumLength(50);
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0).PrecisionScale(18, 4, true);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0).PrecisionScale(18, 4, true);
        RuleFor(x => x.ReorderPoint).GreaterThanOrEqualTo(0);
    }
}

internal sealed class UpdateProductCommandHandler(IWmsDbContext context, ITenantContext tenantContext)
    : IRequestHandler<UpdateProductCommand>
{
    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to update products.");

        var product = await context.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");
        if (!product.IsActive)
            throw new ConflictException("Inactive products cannot be edited.");

        product.Update(
            request.Name.Trim(),
            request.Description?.Trim(),
            request.UnitOfMeasure.Trim(),
            request.UnitCost,
            request.UnitPrice,
            request.ReorderPoint);
        await context.SaveChangesAsync(cancellationToken);
    }
}
