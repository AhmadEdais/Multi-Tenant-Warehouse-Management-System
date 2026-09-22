namespace WMS.Application.Features.Tenants.Commands;

public record ReactivateTenantCommand(int Id) : IRequest;

public sealed class ReactivateTenantCommandValidator : AbstractValidator<ReactivateTenantCommand>
{
    public ReactivateTenantCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

internal sealed class ReactivateTenantCommandHandler(IWmsDbContext context)
    : IRequestHandler<ReactivateTenantCommand>
{
    public async Task Handle(ReactivateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await context.Tenants.FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException($"Tenant with Id {request.Id} not found.");

        if (tenant.IsActive)
        {
            throw new ConflictException($"Tenant with Id {request.Id} is already active.");
        }

        tenant.Reactivate();
        await context.SaveChangesAsync(cancellationToken);
    }
}
