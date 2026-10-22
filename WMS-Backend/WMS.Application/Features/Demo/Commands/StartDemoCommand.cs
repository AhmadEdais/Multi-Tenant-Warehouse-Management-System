using Microsoft.Extensions.Options;
using WMS.Application.Features.Demo.Interfaces;

namespace WMS.Application.Features.Demo.Commands;

public sealed record StartDemoCommand : IRequest<AuthenticationResult>;

internal sealed class StartDemoCommandHandler(IWmsDbContext context, ICurrentUserService currentUser,
    ITenantContext tenantContext, ITenantProvisioningService provisioning, IDemoDataSeeder seeder,
    IAuthSessionIssuer issuer, IDemoCapacityGuard capacity, IOptions<DemoOptions> options)
    : IRequestHandler<StartDemoCommand, AuthenticationResult>
{
    public async Task<AuthenticationResult> Handle(StartDemoCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.IsAuthenticated || currentUser.UserId.HasValue || tenantContext.TenantId.HasValue
            || tenantContext.IsSystemRequest)
            throw new ForbiddenException("Live demos can only be started by guests.");

        if (!options.Value.Enabled)
            throw new ServiceUnavailableException("The live demo is currently unavailable.");

        await using var transaction = await context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.ReadCommitted, cancellationToken);
        await capacity.CheckBeforeProvisioningAsync(options.Value.MaxActiveTenants, cancellationToken);
        // Disposal rolls back an uncommitted transaction; no catch/rollback can mask the original error.
        var result = await provisioning.CreateDemoAsync(cancellationToken);
        await seeder.SeedAsync(result.Tenant.Id, result.AdminUser.Id, cancellationToken);
        var authentication = await issuer.IssueAsync(result.AdminUser, [Roles.TenantAdmin], false,
            result.Tenant.DemoExpiresAtUtc, cancellationToken);
        var sessionValid = await context.AuthSessions.AsNoTracking()
            .Where(s => s.UserId == result.AdminUser.Id)
            .Select(s => !s.RememberMe && s.RevokedAtUtc == null
                && s.AbsoluteExpiresAtUtc <= result.Tenant.DemoExpiresAtUtc
                && s.IdleExpiresAtUtc <= s.AbsoluteExpiresAtUtc
                && s.RefreshTokens.Count == 1
                && s.RefreshTokens.All(t => t.ExpiresAtUtc <= s.IdleExpiresAtUtc && t.ConsumedAtUtc == null))
            .SingleAsync(cancellationToken);
        if (!sessionValid || result.AdminUser.TenantId != result.Tenant.Id
            || result.AdminUser.UserRoles.Count != 1)
            throw new InvalidOperationException("Demo authentication consistency verification failed.");
        await capacity.CheckBeforeCommitAsync(options.Value.MaxActiveTenants, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return authentication;
    }
}
