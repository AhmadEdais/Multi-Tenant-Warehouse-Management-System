namespace WMS.Application.Features.Locations.Queries;

public record GetLocationTreeQuery(int WarehouseId) : IRequest<List<LocationTreeDto>>;
public class LocationTreeDto : ITreeNode<LocationTreeDto>
{
    public int Id { get; set; }
    public int? ParentLocationId { get; set; }
    public string LocationType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal? MaxWeightCapacityKg { get; set; }
    public int Level { get; set; }

    public List<LocationTreeDto> Children { get; set; } = [];
    [JsonIgnore]
    public int? ParentId => ParentLocationId;
}
public class GetLocationTreeQueryHandler(IWmsDbContext context, ITenantContext tenantContext)
        : IRequestHandler<GetLocationTreeQuery, List<LocationTreeDto>>
{
    public async Task<List<LocationTreeDto>> Handle(GetLocationTreeQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        var flatLocations = await context.Database.SqlQuery<LocationTreeDto>($@"
                WITH LocationCTE AS
                (
                    -- ANCHOR: Get root locations (Zones) for this specific warehouse
                    SELECT Id, ParentLocationId, LocationType, Name, Barcode, MaxWeightCapacityKg, 0 AS Level
                    FROM Locations
                    WHERE ParentLocationId IS NULL 
                      AND WarehouseId = {request.WarehouseId}
                      AND TenantId = {tenantId}
                      AND IsActive = CAST(1 AS BIT)

                    UNION ALL

                    -- RECURSION: Get children (Aisles -> Racks -> Bins)
                    SELECT l.Id, l.ParentLocationId, l.LocationType, l.Name, l.Barcode, l.MaxWeightCapacityKg, cte.Level + 1 AS Level
                    FROM Locations l
                    INNER JOIN LocationCTE cte ON l.ParentLocationId = cte.Id
                    WHERE l.WarehouseId = {request.WarehouseId}
                      AND l.TenantId = {tenantId}
                      AND l.IsActive = CAST(1 AS BIT)
                )
                SELECT Id, ParentLocationId, LocationType, Name, Barcode, MaxWeightCapacityKg, Level
                FROM LocationCTE
                ORDER BY Level, Name"
        ).ToListAsync(cancellationToken);

        return flatLocations.BuildTree();
    }
}