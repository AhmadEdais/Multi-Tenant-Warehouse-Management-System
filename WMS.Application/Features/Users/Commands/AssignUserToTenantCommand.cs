namespace WMS.Application.Features.Users.Commands;
public record AssignUserToTenantDto(int TenantId);
public record AssignUserToTenantCommand(int UserId, int TenantId) : IRequest;

public sealed class AssignUserToTenantCommandValidator : AbstractValidator<AssignUserToTenantCommand>
{
    public AssignUserToTenantCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.TenantId).GreaterThan(0);
    }
}
internal sealed class AssignUserToTenantCommandHandler(
    IWmsDbContext context,
    ITenantContext tenantContext) : IRequestHandler<AssignUserToTenantCommand>
{
    private readonly IWmsDbContext _context = context;
    private readonly ITenantContext _tenantContext = tenantContext;

    public async Task Handle(AssignUserToTenantCommand request, CancellationToken cancellationToken)
    {

        if (!_tenantContext.IsSystemRequest)
        {
            throw new UnauthorizedAccessException("Only Global System Admins can explicitly reassign a user's tenant.");
        }

        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException($"User with ID {request.UserId} not found.");

        var tenantExists = await _context.Tenants
            .AnyAsync(t => t.Id == request.TenantId, cancellationToken);

        if (!tenantExists) throw new NotFoundException($"Tenant with ID {request.TenantId} not found.");

        user.AssignToTenant(request.TenantId);

        await _context.SaveChangesAsync(cancellationToken);
    }
}