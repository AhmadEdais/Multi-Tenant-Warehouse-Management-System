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
    public async Task Handle(AssignProductCategoriesCommand request, CancellationToken cancellationToken)
    {
        var product = await context.Products
                .Include(p => p.ProductCategories)
                .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken)
                ?? throw new NotFoundException($"Product with ID {request.ProductId} not found.");

        var validCategories = await context.Categories
            .Where(c => request.CategoryIds.Contains(c.Id))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        if (validCategories.Count != request.CategoryIds.Distinct().Count())
        {
            throw new ValidationException("One or more Category IDs are invalid.");
        }

        if (product.ProductCategories.Count != 0)
        {
            context.ProductCategories.RemoveRange(product.ProductCategories);
        }

        var newMappings = validCategories.Select(categoryId =>
            ProductCategory.Create(product.Id, categoryId));

        context.ProductCategories.AddRange(newMappings);

        await context.SaveChangesAsync(cancellationToken);
    }
}