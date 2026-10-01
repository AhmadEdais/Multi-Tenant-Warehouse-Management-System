namespace WMS.Application.Features.Categories.Queries;

public record GetCategoryTreeQuery() : IRequest<List<CategoryTreeDto>>;
public class CategoryTreeDto : ITreeNode<CategoryTreeDto>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public bool IsActive { get; set; }
    public int Level { get; set; }
    public List<CategoryTreeDto> Children { get; set; } = [];
    [JsonIgnore]
    public int? ParentId => ParentCategoryId;       

}
internal class GetCategoryTreeQueryHandler(
    IWmsDbContext context,
    ITenantContext tenantContext,
    ICurrentUserService currentUser) : IRequestHandler<GetCategoryTreeQuery, List<CategoryTreeDto>>
{
    public async Task<List<CategoryTreeDto>> Handle(GetCategoryTreeQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedAccessException("A tenant workspace is required to view categories.");
        var includeInactive = currentUser.IsInRole(Roles.TenantAdmin)
            || currentUser.IsInRole(Roles.WarehouseManager);
        var flatCategories = await context.Database.SqlQuery<CategoryTreeDto>($@"
                WITH CategoryTreeHierarchy AS
                (
                    -- Anchor
                    SELECT Id, Name, ParentCategoryId, IsActive, 0 AS Level
                    FROM Categories
                    WHERE ParentCategoryId IS NULL 
                      AND TenantId = {tenantId} 
                      AND ({includeInactive} = CAST(1 AS BIT) OR IsActive = CAST(1 AS BIT))

                    UNION ALL

                    -- Recursion
                    SELECT c.Id, c.Name, c.ParentCategoryId, c.IsActive, CTH.Level + 1 AS Level
                    FROM Categories c
                    INNER JOIN CategoryTreeHierarchy CTH ON c.ParentCategoryId = CTH.Id
                    WHERE c.TenantId = {tenantId} 
                      AND ({includeInactive} = CAST(1 AS BIT) OR c.IsActive = CAST(1 AS BIT))
                )
                SELECT Id, Name, ParentCategoryId, IsActive, Level
                FROM CategoryTreeHierarchy
                ORDER BY Level, Name
                OPTION (MAXRECURSION 0)"
            ).ToListAsync(cancellationToken);
        return flatCategories.BuildTree();
    }
}
