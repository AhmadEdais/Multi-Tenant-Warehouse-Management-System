namespace WMS.Application.Features.Products.Commands;

public record AssignProductCategoriesCommand(
        int ProductId,
        List<int> CategoryIds) : IRequest;

public class AssignProductCategoriesCommandValidator : AbstractValidator<AssignProductCategoriesCommand>
{
    public AssignProductCategoriesCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0);
        RuleFor(x => x.CategoryIds)
            .NotEmpty()
            .Must(ids => ids.All(id => id > 0))
            .WithMessage("All category IDs must be greater than 0.");
    }
}
internal class AssignProductCategoriesCommandHandler(IWmsDbContext context) : IRequestHandler<AssignProductCategoriesCommand>
{
    private readonly IWmsDbContext _context = context;
    public async Task Handle(AssignProductCategoriesCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
                .Include(p => p.ProductCategories)
                .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken)
                ?? throw new NotFoundException($"Product with ID {request.ProductId} not found.");

        var validCategories = await _context.Categories
            .Where(c => request.CategoryIds.Contains(c.Id))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        if (validCategories.Count != request.CategoryIds.Distinct().Count())
        {
            throw new ValidationException("One or more Category IDs are invalid.");
        }

        if (product.ProductCategories.Count != 0)
        {
            _context.ProductCategories.RemoveRange(product.ProductCategories);
        }

        var newMappings = validCategories.Select(categoryId =>
            ProductCategory.Create(product.Id, categoryId));

        _context.ProductCategories.AddRange(newMappings);

        await _context.SaveChangesAsync(cancellationToken);
    }
}