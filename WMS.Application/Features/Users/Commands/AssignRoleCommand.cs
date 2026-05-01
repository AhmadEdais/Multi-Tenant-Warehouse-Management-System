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

internal sealed class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand>
{
    private readonly IWmsDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantContext _tenantContext; 

    public AssignRoleCommandHandler(
        IWmsDbContext context,
        ICurrentUserService currentUserService,
        ITenantContext tenantContext) 
    {
        _context = context;
        _currentUserService = currentUserService;
        _tenantContext = tenantContext;
    }

    public async Task Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var currentAdminId = _currentUserService.UserId; 

        var targetUser = await _context.Users
            .Include(u => u.UserRoles)
            .Where(u => u.Id == request.TargetUserId && u.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (targetUser == null) throw new NotFoundException("User not found.");

        var roleExists = await _context.Roles.AnyAsync(r => r.Id == request.RoleId, cancellationToken);
        if (!roleExists) throw new NotFoundException("Role not found.");

        targetUser.AssignRole(request.RoleId, currentAdminId);

        await _context.SaveChangesAsync(cancellationToken);
    }
}