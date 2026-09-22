namespace WMS.Application.Features.Users.Commands;

public record ReactivateUserCommand(int Id) : IRequest;

public sealed class ReactivateUserCommandValidator : AbstractValidator<ReactivateUserCommand>
{
    public ReactivateUserCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

internal sealed class ReactivateUserCommandHandler(IWmsDbContext context, ITenantContext tenantContext)
    : IRequestHandler<ReactivateUserCommand>
{
    public async Task Handle(ReactivateUserCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedAccessException("A tenant workspace is required to reactivate users.");

        var user = await context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == request.Id && u.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException($"User with Id {request.Id} not found.");

        if (user.IsActive)
        {
            throw new ConflictException($"User with Id {request.Id} is already active.");
        }

        user.Reactivate();
        await context.SaveChangesAsync(cancellationToken);
    }
}
