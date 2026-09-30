namespace WMS.Application.Features.Locations.Queries;

public record LocationDetailsDto(
    int Id,
    int WarehouseId,
    string WarehouseName,
    string WarehouseCode,
    int? ParentLocationId,
    string? ParentLocationName,
    string LocationType,
    string Name,
    string? Barcode,
    decimal? MaxWeightCapacityKg,
    bool IsActive);

public record GetLocationByIdQuery(int Id) : IRequest<LocationDetailsDto>;

public sealed class GetLocationByIdQueryValidator : AbstractValidator<GetLocationByIdQuery>
{
    public GetLocationByIdQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

internal sealed class GetLocationByIdQueryHandler(IWmsDbContext context)
    : IRequestHandler<GetLocationByIdQuery, LocationDetailsDto>
{
    public async Task<LocationDetailsDto> Handle(GetLocationByIdQuery request, CancellationToken cancellationToken)
    {
        var location = await context.Locations
            .AsNoTracking()
            .Where(l => l.Id == request.Id)
            .Select(l => new LocationDetailsDto(
                l.Id,
                l.WarehouseId,
                l.Warehouse!.Name,
                l.Warehouse.Code,
                l.ParentLocationId,
                l.ParentLocation == null ? null : l.ParentLocation.Name,
                l.LocationType,
                l.Name,
                l.Barcode,
                l.MaxWeightCapacityKg,
                l.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return location ?? throw new NotFoundException($"Location with ID {request.Id} not found.");
    }
}
