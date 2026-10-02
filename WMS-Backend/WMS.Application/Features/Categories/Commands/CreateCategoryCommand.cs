namespace WMS.Application.Features.Categories.Commands
{
    public record CreateCategoryCommand(
        string Name,
        int? ParentCategoryId) : IRequest<int>;
    public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
    {
        public CreateCategoryCommandValidator()
        {
            RuleFor(x => x.Name)
                .Must(name => !string.IsNullOrWhiteSpace(name))
                .MaximumLength(200);

            RuleFor(x => x.ParentCategoryId)
                .GreaterThan(0)
                .When(x => x.ParentCategoryId.HasValue);
        }
    }
    internal class CreateCategoryCommandHandler(IWmsDbContext context, ITenantContext tenantContext) : IRequestHandler<CreateCategoryCommand, int>
    {
        public async Task<int> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            if (!tenantContext.TenantId.HasValue)
                throw new UnauthorizedAccessException("A tenant workspace is required to create categories.");
            await using var transaction = await context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, cancellationToken);
            if (request.ParentCategoryId.HasValue)
            {
                await CategoryHierarchy.ValidateActiveParentAsync(
                    context, request.ParentCategoryId.Value, cancellationToken);
            }
            var category = Category.Create(request.Name.Trim(), request.ParentCategoryId);

            context.Categories.Add(category);

            await context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return category.Id;
        }
    }
}
