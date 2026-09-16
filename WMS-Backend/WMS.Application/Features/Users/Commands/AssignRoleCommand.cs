namespace WMS.Application.Features.Users.Commands;
public record AssignRoleDto(int RoleId);
public record AssignRoleCommand(int TargetUserId, int RoleId) : IRequest;

internal sealed class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleCommandValidator()
    {
        RuleFor(x => x.TargetUserId).GreaterThan(0).WithMessage("Target User Id must be greater than 0.");
        RuleFor(x => x.RoleId).GreaterThan(0).WithMessage("Role Id must be greater than 0.");
    }
}

internal sealed class AssignRoleCommandHandler(
    IWmsDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<AssignRoleCommand>
{ 
    public async Task Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var currentAdminId = currentUserService.UserId; 

        var targetUser = await context.Users
            .Include(u => u.UserRoles)
            .Where(u => u.Id == request.TargetUserId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var roleExists = await context.Roles.AnyAsync(r => r.Id == request.RoleId, cancellationToken);
        if (!roleExists) throw new NotFoundException("Role not found.");

        targetUser.AssignRole(request.RoleId, currentAdminId);

        await context.SaveChangesAsync(cancellationToken);
    }
}