namespace WMS.Application.Features.Categories;

internal static class CategoryHierarchy
{
    public static async Task ValidateActiveParentAsync(
        IWmsDbContext context,
        int parentCategoryId,
        CancellationToken cancellationToken)
    {
        var parent = await context.Categories
            .AsNoTracking()
            .Where(c => c.Id == parentCategoryId)
            .Select(c => new { c.IsActive })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Parent category was not found.");

        if (!parent.IsActive)
            throw new ConflictException("The selected parent category is inactive.");
    }
}
