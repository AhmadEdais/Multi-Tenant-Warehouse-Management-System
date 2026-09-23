namespace WMS.Application.Features.Users.Commands;

public record ReplaceUserRolesDto(IReadOnlyCollection<int> RoleIds);

public record ReplaceUserRolesCommand(int TargetUserId, IReadOnlyCollection<int> RoleIds) : IRequest;

public sealed class ReplaceUserRolesCommandValidator : AbstractValidator<ReplaceUserRolesCommand>
{
    public ReplaceUserRolesCommandValidator()
    {
        RuleFor(x => x.TargetUserId)
            .GreaterThan(0).WithMessage("Target user Id must be greater than zero.");

        RuleFor(x => x.RoleIds)
            .NotNull().WithMessage("At least one role is required.")
            .NotEmpty().WithMessage("At least one role is required.")
            .Must(roleIds => roleIds is null || roleIds.Count == roleIds.Distinct().Count())
            .WithMessage("Role IDs must be unique.");

        RuleForEach(x => x.RoleIds)
            .GreaterThan(0).WithMessage("Role IDs must be greater than zero.");
    }
}

internal sealed class ReplaceUserRolesCommandHandler(
    IWmsDbContext context,
    ITenantContext tenantContext,
    ICurrentUserService currentUser) : IRequestHandler<ReplaceUserRolesCommand>
{
    public async Task Handle(ReplaceUserRolesCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedAccessException("A tenant workspace is required to replace user roles.");
        var tenantAdminId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("An authenticated TenantAdmin is required to replace user roles.");

        var user = await context.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == request.TargetUserId && u.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException($"User with Id {request.TargetUserId} not found.");

        if (user.Id == tenantAdminId)
        {
            throw new ConflictException("You cannot replace your own roles.");
        }

        var requestedRoleIds = request.RoleIds.ToArray();
        var roles = await context.Roles
            .Where(r => requestedRoleIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        if (roles.Count != requestedRoleIds.Length)
        {
            throw new NotFoundException("One or more selected roles do not exist.");
        }

        if (roles.Any(r => !Roles.TenantAssignable.Contains(r.Name)))
        {
            throw new UnauthorizedAccessException("One or more selected roles are not assignable tenant roles.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        context.UserRoles.RemoveRange(user.UserRoles.ToList());
        await context.SaveChangesAsync(cancellationToken);

        user.ReplaceRoles(requestedRoleIds, tenantAdminId);
        await context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
