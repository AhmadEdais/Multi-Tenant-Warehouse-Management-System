namespace WMS.Application.Features.Categories.Commands;

public record UpdateCategoryCommand(
    [property: JsonIgnore] int Id,
    string Name,
    int? ParentCategoryId) : IRequest;

internal sealed class DescendantCategoryId
{
    public int Id { get; set; }
}

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).Must(name => !string.IsNullOrWhiteSpace(name)).MaximumLength(200);
        RuleFor(x => x.ParentCategoryId).GreaterThan(0).When(x => x.ParentCategoryId.HasValue);
    }
}

internal sealed class UpdateCategoryCommandHandler(IWmsDbContext context, ITenantContext tenantContext)
    : IRequestHandler<UpdateCategoryCommand>
{
    public async Task Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedAccessException("A tenant workspace is required to update categories.");

        await using var transaction = await context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);

        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Category was not found.");
        if (!category.IsActive)
            throw new ConflictException("Inactive categories cannot be edited.");

        if (request.ParentCategoryId == category.Id)
            throw new ConflictException("A category cannot be its own parent.");

        if (request.ParentCategoryId.HasValue)
        {
            await CategoryHierarchy.ValidateActiveParentAsync(
                context, request.ParentCategoryId.Value, cancellationToken);

            var descendantsContainParent = await context.Database.SqlQuery<DescendantCategoryId>($@"
                WITH Descendants AS
                (
                    SELECT Id
                    FROM dbo.Categories
                    WHERE ParentCategoryId = {category.Id} AND TenantId = {tenantId}

                    UNION ALL

                    SELECT c.Id
                    FROM dbo.Categories c
                    INNER JOIN Descendants d ON c.ParentCategoryId = d.Id
                    WHERE c.TenantId = {tenantId}
                )
                SELECT Id FROM Descendants WHERE Id = {request.ParentCategoryId.Value}
                OPTION (MAXRECURSION 256)")
                .ToListAsync(cancellationToken);

            if (descendantsContainParent.Count > 0)
                throw new ConflictException("A category cannot be moved under one of its descendants.");
        }

        category.Update(request.Name.Trim(), request.ParentCategoryId);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
