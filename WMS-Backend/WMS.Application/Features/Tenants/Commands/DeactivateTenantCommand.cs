namespace WMS.Application.Features.Tenants.Commands;

public record DeactivateTenantCommand(int Id) : IRequest;

public sealed class DeactivateTenantCommandValidator : AbstractValidator<DeactivateTenantCommand>
{
    public DeactivateTenantCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

internal sealed class DeactivateTenantCommandHandler(IWmsDbContext context)
    : IRequestHandler<DeactivateTenantCommand>
{
    public async Task Handle(DeactivateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await context.Tenants.FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException($"Tenant with Id {request.Id} not found.");

        if (!tenant.IsActive)
        {
            throw new ConflictException($"Tenant with Id {request.Id} is already deactivated.");
        }

        tenant.Deactivate();
        await context.SaveChangesAsync(cancellationToken);
    }
}
