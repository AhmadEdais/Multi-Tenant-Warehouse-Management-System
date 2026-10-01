namespace WMS.Application.Features.Locations.Commands;

public record DeactivateLocationCommand(int Id) : IRequest;
public class DeactivateLocationCommandValidator : AbstractValidator<DeactivateLocationCommand>
{
    public DeactivateLocationCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
internal class DeactivateLocationCommandHandler(IWmsDbContext context) : IRequestHandler<DeactivateLocationCommand>
{
    public async Task Handle(DeactivateLocationCommand request, CancellationToken cancellationToken)
    {
        var location = await context.Locations
            .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Location with ID {request.Id} not found.");
        if (!location.IsActive)
            throw new ConflictException("Location is already inactive.");
        var hasActiveChildLocations = await context.Locations.AnyAsync(l => l.ParentLocationId == request.Id && l.IsActive, cancellationToken);
        if (hasActiveChildLocations)
        {
            throw new ConflictException("This location has active child locations and cannot be deactivated.");
        }
        if (location.LocationType == LocationTypes.Bin &&
            await context.StockLevels.AnyAsync(s => s.LocationId == location.Id &&
                (s.QuantityOnHand > 0 || s.QuantityAllocated > 0), cancellationToken))
            throw new ConflictException("This bin still contains stock and cannot be deactivated.");
        location.Deactivate();
        await context.SaveChangesAsync(cancellationToken);  
    }
}
