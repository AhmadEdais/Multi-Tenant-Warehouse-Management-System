namespace WMS.Application.Features.Locations.Commands;

public record ReactivateLocationCommand(int Id) : IRequest;

public sealed class ReactivateLocationCommandValidator : AbstractValidator<ReactivateLocationCommand>
{
    public ReactivateLocationCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

internal sealed class ReactivateLocationCommandHandler(IWmsDbContext context)
    : IRequestHandler<ReactivateLocationCommand>
{
    public async Task Handle(ReactivateLocationCommand request, CancellationToken cancellationToken)
    {
        var location = await context.Locations
            .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Location with ID {request.Id} not found.");
        if (location.IsActive)
            throw new ConflictException("Location is already active.");

        await LocationHierarchy.ValidateParentAsync(
            context, location.WarehouseId, location.LocationType, location.ParentLocationId, cancellationToken);

        location.Reactivate();
        await context.SaveChangesAsync(cancellationToken);
    }
}
