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
            .Must(ids => ids.All(id => id > 0))
            .WithMessage("All category IDs must be greater than 0.")
            .When(x => x.CategoryIds is not null);
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
        if (!product.IsActive)
            throw new ConflictException("Inactive products cannot have their categories changed.");

        var requestedIds = request.CategoryIds.ToHashSet();
        var existingIds = product.ProductCategories.Select(pc => pc.CategoryId).ToHashSet();
        var addedIds = requestedIds.Except(existingIds).ToArray();

        var validAddedCount = await context.Categories
            .CountAsync(c => c.IsActive && addedIds.Contains(c.Id), cancellationToken);
        if (validAddedCount != addedIds.Length)
        {
            throw new ValidationException("One or more Category IDs are invalid or inactive.");
        }

        var removedMappings = product.ProductCategories
            .Where(pc => !requestedIds.Contains(pc.CategoryId))
            .ToList();
        context.ProductCategories.RemoveRange(removedMappings);
        var newMappings = addedIds.Select(categoryId =>
            ProductCategory.Create(product.Id, categoryId));

        context.ProductCategories.AddRange(newMappings);

        await context.SaveChangesAsync(cancellationToken);
    }
}
