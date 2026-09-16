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
        var location = await context.Locations.FindAsync([new [] { request.Id }, cancellationToken], cancellationToken: cancellationToken);
        if (location == null || !location.IsActive)
        {
            throw new NotFoundException($"Location with ID {request.Id} not found or already inactive.");
        }
        var hasActiveChildLocations = await context.Locations.AnyAsync(l => l.ParentLocationId == request.Id && l.IsActive, cancellationToken);
        if (hasActiveChildLocations)
        {
            throw new ConflictException($"Location with ID {request.Id} has active child locations and cannot be deactivated.");
        }
        location.Deactivate();
        await context.SaveChangesAsync(cancellationToken);  
    }
}