namespace WMS.Application.Features.Locations;

internal static class LocationHierarchy
{
    public static async Task ValidateParentAsync(
        IWmsDbContext context,
        int warehouseId,
        string locationType,
        int? parentLocationId,
        CancellationToken cancellationToken)
    {
        if (!LocationTypes.IsAddable(locationType))
            throw new ConflictException("Location type must be Zone, Aisle, Rack, or Bin.");
        var requiredParentType = LocationTypes.RequiredParentType(locationType);
        if (requiredParentType is null)
        {
            if (parentLocationId.HasValue)
                throw new ConflictException("A Zone cannot have a parent location.");
            return;
        }

        if (!parentLocationId.HasValue)
            throw new ConflictException($"A {locationType} must belong to a {requiredParentType}.");

        var parent = await context.Locations
            .AsNoTracking()
            .Where(l => l.Id == parentLocationId.Value)
            .Select(l => new { l.WarehouseId, l.LocationType, l.IsActive })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Parent location was not found.");

        if (parent.WarehouseId != warehouseId)
            throw new ConflictException("Parent location must belong to the same warehouse.");
        if (!parent.IsActive)
            throw new ConflictException("The selected parent is inactive.");
        if (parent.LocationType != requiredParentType)
            throw new ConflictException($"A {locationType} must belong to a {requiredParentType}.");
    }
}
