namespace WMS.Application.Features.Categories.Commands;

public record DeactivateCategoryCommand(int Id) : IRequest;

public sealed class DeactivateCategoryCommandValidator : AbstractValidator<DeactivateCategoryCommand>
{
    public DeactivateCategoryCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

internal sealed class DeactivateCategoryCommandHandler(IWmsDbContext context, ITenantContext tenantContext)
    : IRequestHandler<DeactivateCategoryCommand>
{
    public async Task Handle(DeactivateCategoryCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to deactivate categories.");
        await using var transaction = await context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);
        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Category was not found.");
        if (!category.IsActive)
            throw new ConflictException("Category is already inactive.");

        if (await context.Categories.AnyAsync(
            c => c.ParentCategoryId == category.Id && c.IsActive, cancellationToken))
            throw new ConflictException("This category has active child categories and cannot be deactivated.");

        category.Deactivate();
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
