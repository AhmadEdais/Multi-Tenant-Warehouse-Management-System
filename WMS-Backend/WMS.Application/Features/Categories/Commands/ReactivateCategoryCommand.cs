namespace WMS.Application.Features.Categories.Commands;

public record ReactivateCategoryCommand(int Id) : IRequest;

public sealed class ReactivateCategoryCommandValidator : AbstractValidator<ReactivateCategoryCommand>
{
    public ReactivateCategoryCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

internal sealed class ReactivateCategoryCommandHandler(IWmsDbContext context, ITenantContext tenantContext)
    : IRequestHandler<ReactivateCategoryCommand>
{
    public async Task Handle(ReactivateCategoryCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to reactivate categories.");
        await using var transaction = await context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);
        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Category was not found.");
        if (category.IsActive)
            throw new ConflictException("Category is already active.");

        if (category.ParentCategoryId.HasValue)
            await CategoryHierarchy.ValidateActiveParentAsync(
                context, category.ParentCategoryId.Value, cancellationToken);

        category.Reactivate();
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
