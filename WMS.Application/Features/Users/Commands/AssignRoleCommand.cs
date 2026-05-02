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
    private readonly IWmsDbContext _context = context;
    private readonly ICurrentUserService _currentUserService = currentUserService;
   

    public async Task Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var currentAdminId = _currentUserService.UserId; 

        var targetUser = await _context.Users
            .Include(u => u.UserRoles)
            .Where(u => u.Id == request.TargetUserId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var roleExists = await _context.Roles.AnyAsync(r => r.Id == request.RoleId, cancellationToken);
        if (!roleExists) throw new NotFoundException("Role not found.");

        targetUser.AssignRole(request.RoleId, currentAdminId);

        await _context.SaveChangesAsync(cancellationToken);
    }
}