using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WMS.Application.Features.Demo.Interfaces;

namespace WMS.Application.Features.Demo.Commands;

public sealed record StartDemoCommand : IRequest<AuthenticationResult>;

internal sealed class StartDemoCommandHandler(IWmsDbContext context, ICurrentUserService currentUser,
    ITenantContext tenantContext, ITenantProvisioningService provisioning, IDemoDataSeeder seeder,
    IAuthSessionIssuer issuer, IDemoCapacityGuard capacity, IOptions<DemoOptions> options,
    ILogger<StartDemoCommandHandler> logger) : IRequestHandler<StartDemoCommand, AuthenticationResult>
{
    public async Task<AuthenticationResult> Handle(StartDemoCommand request, CancellationToken cancellationToken)
    {
        // TODO(DEMO-PERF): Remove Stopwatch instrumentation after measuring and optimizing demo provisioning.
        var total = Stopwatch.StartNew();
        var timer = Stopwatch.StartNew();
        var stage = "Guest/security/resource checks";
        try
        {
            if (currentUser.IsAuthenticated || currentUser.UserId.HasValue || tenantContext.TenantId.HasValue
                || tenantContext.IsSystemRequest)
                throw new ForbiddenException("Live demos can only be started by guests.");

            if (!options.Value.Enabled)
                throw new ServiceUnavailableException("The live demo is currently unavailable.");

            await using var transaction = await context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.ReadCommitted, cancellationToken);
            await capacity.CheckBeforeProvisioningAsync(options.Value.MaxActiveTenants, cancellationToken);
            LogStage(stage, timer);
            // Disposal rolls back an uncommitted transaction; no catch/rollback can mask the original error.
            stage = "Tenant provisioning";
            timer.Restart();

            var result = await provisioning.CreateDemoAsync(cancellationToken);
            LogStage(stage, timer);
            stage = "Dataset seeding";
            await seeder.SeedAsync(result.Tenant.Id, result.AdminUser.Id, cancellationToken);
            stage = "Authentication issuance";
            timer.Restart();
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
            LogStage(stage, timer);
            stage = "Final capacity check";
            timer.Restart();
            await capacity.CheckBeforeCommitAsync(options.Value.MaxActiveTenants, cancellationToken);
            LogStage(stage, timer);
            stage = "Transaction commit";
            timer.Restart();
            await transaction.CommitAsync(cancellationToken);
            LogStage(stage, timer);
            return authentication;
        }
        catch
        {
            logger.LogWarning("[Demo Performance] Command failed at stage: {Stage}", stage);
            throw;
        }
        finally { LogStage("Handler total", total); }
    }

    private void LogStage(string stage, Stopwatch timer) => logger.LogInformation(
        "[Demo Performance] {Stage}: {ElapsedMilliseconds:F2} ms", stage, timer.Elapsed.TotalMilliseconds);
}
