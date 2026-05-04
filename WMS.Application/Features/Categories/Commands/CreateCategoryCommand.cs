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
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.ParentCategoryId)
                .GreaterThan(0)
                .When(x => x.ParentCategoryId.HasValue);
        }
    }
    internal class CreateCategoryCommandHandler(IWmsDbContext context) : IRequestHandler<CreateCategoryCommand, int>
    {
        public async Task<int> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            if (request.ParentCategoryId.HasValue)
            {
                var existingParentCategory = await context.Categories
                    .AnyAsync(c => c.Id == request.ParentCategoryId.Value, cancellationToken);
                if (!existingParentCategory)
                {
                    throw new InvalidOperationException("The specified parent category does not exist.");
                }
            }
            var category = Category.Create(request.Name, request.ParentCategoryId);

            context.Categories.Add(category);

            await context.SaveChangesAsync(cancellationToken);

            return category.Id;
        }
    }
}