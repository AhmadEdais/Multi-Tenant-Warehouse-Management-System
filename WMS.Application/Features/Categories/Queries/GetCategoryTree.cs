namespace WMS.Application.Features.Categories.Queries;

public record GetCategoryTreeQuery() : IRequest<List<CategoryTreeDto>>;
public class CategoryTreeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public int Level { get; set; }

    // This is where we will nest the children in C#
    public List<CategoryTreeDto> Children { get; set; } = [];
}
internal class GetCategoryTreeQueryHandler(IWmsDbContext context, ITenantContext tenantContext) : IRequestHandler<GetCategoryTreeQuery, List<CategoryTreeDto>>
{
    private readonly IWmsDbContext _context = context;
    private readonly ITenantContext _tenantContext = tenantContext;
    public async Task<List<CategoryTreeDto>> Handle(GetCategoryTreeQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var flatCategories = await _context.Database.SqlQuery<CategoryTreeDto>($@"
                WITH CategoryTreeHierarchy AS
                (
                    -- Anchor
                    SELECT Id, Name, ParentCategoryId, 0 AS Level
                    FROM Categories
                    WHERE ParentCategoryId IS NULL 
                      AND TenantId = {tenantId} 
                      AND IsActive = CAST(1 AS BIT)

                    UNION ALL

                    -- Recursion
                    SELECT c.Id, c.Name, c.ParentCategoryId, CTH.Level + 1 AS Level
                    FROM Categories c
                    INNER JOIN CategoryTreeHierarchy CTH ON c.ParentCategoryId = CTH.Id
                    WHERE c.TenantId = {tenantId} 
                      AND c.IsActive = CAST(1 AS BIT)
                )
                SELECT Id, Name, ParentCategoryId, Level
                FROM CategoryTreeHierarchy
                ORDER BY Level, Name"
            ).ToListAsync(cancellationToken);
        return BuildTree(flatCategories);
    }
    private static List<CategoryTreeDto> BuildTree(List<CategoryTreeDto> flatCategories)
    {
        var lookup = flatCategories.ToDictionary(c => c.Id);
        var rootCategories = new List<CategoryTreeDto>();
        foreach (var category in flatCategories)
        {
            if (category.ParentCategoryId.HasValue)
            {
                if (lookup.TryGetValue(category.ParentCategoryId.Value, out var parent))
                {
                    parent.Children.Add(category);
                }
            }
            else
            {
                rootCategories.Add(category);
            }
        }
        return rootCategories;
    }
}