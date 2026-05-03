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
            .GreaterThanOrEqualTo(0);
        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReorderPoint)
            .GreaterThanOrEqualTo(0);
    }
}
internal class CreateProductCommandHandler(IWmsDbContext context, ITenantContext tenantContext) : IRequestHandler<CreateProductCommand, int>
{
    private readonly IWmsDbContext _context = context;
    private readonly ITenantContext _tenantContext = tenantContext;
    public async Task<int> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var existingProduct = await _context.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.SKU == request.SKU && p.TenantId == tenantId, cancellationToken);
        if (existingProduct != null)
        {
            throw new InvalidOperationException("A product with the same SKU already exists.");
        }
        var product = Product.Create(
            request.SKU,
            request.Name,
            request.Description,
            request.UnitOfMeasure,
            request.UnitCost,
            request.UnitPrice,
            request.ReorderPoint);
        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);
        return product.Id;
    }
}