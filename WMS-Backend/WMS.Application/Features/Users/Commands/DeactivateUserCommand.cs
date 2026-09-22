namespace WMS.Application.Features.Users.Commands;

public record DeactivateUserCommand(int Id) : IRequest;

public sealed class DeactivateUserCommandValidator : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

internal sealed class DeactivateUserCommandHandler(IWmsDbContext context, ITenantContext tenantContext)
    : IRequestHandler<DeactivateUserCommand>
{
    public async Task Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedAccessException("A tenant workspace is required to deactivate users.");

        var user = await context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == request.Id && u.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException($"User with Id {request.Id} not found.");

        if (!user.IsActive)
        {
            throw new ConflictException($"User with Id {request.Id} is already deactivated.");
        }

        user.Deactivate();
        await context.SaveChangesAsync(cancellationToken);
    }
}
