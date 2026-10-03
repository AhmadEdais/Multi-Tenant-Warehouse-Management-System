namespace WMS.Application.Features.Products.Commands;

public record CreateProductCommand(
    string SKU,
    string Name,
    string? Description,
    string UnitOfMeasure,
    decimal UnitCost,
    decimal UnitPrice,
    int ReorderPoint) : IRequest<int>;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
    {
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.SKU)
            .NotEmpty()
            .MaximumLength(100);
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
        RuleFor(x => x.Description)
            .MaximumLength(1000);
        RuleFor(x => x.UnitOfMeasure)
            .NotEmpty()
            .MaximumLength(50);
        RuleFor(x => x.UnitCost)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(18, 4, true);
        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(18, 4, true);
        RuleFor(x => x.ReorderPoint)
            .GreaterThanOrEqualTo(0);
    }
}
internal class CreateProductCommandHandler(IWmsDbContext context, ITenantContext tenantContext) : IRequestHandler<CreateProductCommand, int>
{
    public async Task<int> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to create products.");

        var sku = request.SKU.Trim();
        var name = request.Name.Trim();
        var description = request.Description?.Trim();
        var unitOfMeasure = request.UnitOfMeasure.Trim();

        if (await context.Products.AnyAsync(p => p.SKU == sku, cancellationToken))
        {
            throw new ConflictException("A product with the same SKU already exists.");
        }
        var product = Product.Create(
            sku,
            name,
            description,
            unitOfMeasure,
            request.UnitCost,
            request.UnitPrice,
            request.ReorderPoint);
        context.Products.Add(product);
        await context.SaveChangesAsync(cancellationToken);
        return product.Id;
    }
}
