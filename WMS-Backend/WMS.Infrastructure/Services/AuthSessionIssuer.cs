using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace WMS.Infrastructure.Services;

internal sealed class AuthSessionIssuer(IWmsDbContext context, IJwtProvider jwtProvider,
    IRefreshTokenGenerator refreshTokenGenerator, IOptions<AuthSessionPolicy> policy,
    ILogger<AuthSessionIssuer> logger) : IAuthSessionIssuer
{
    public async Task<AuthenticationResult> IssueAsync(User user, IReadOnlyList<string> roles,
        bool rememberMe, DateTime? tenantDeadlineUtc, CancellationToken cancellationToken)
    {
        // TODO(DEMO-PERF): Remove Stopwatch instrumentation after profiling shared session issuance.
        var timer = Stopwatch.StartNew();
        var now = DateTime.UtcNow;
        var (idle, absolute) = policy.Value.GetDeadlines(now, rememberMe);
        if (tenantDeadlineUtc.HasValue && tenantDeadlineUtc.Value < absolute)
            absolute = tenantDeadlineUtc.Value;
        if (idle > absolute) idle = absolute;
        var session = AuthSession.Create(user.Id, now, idle, absolute, rememberMe);
        var (plaintext, hash) = refreshTokenGenerator.Generate();
        session.AddInitialRefreshToken(hash, idle);
        user.RecordLogin();
        context.AuthSessions.Add(session);
        logger.LogInformation("[Authentication Performance] Session construction: {ElapsedMilliseconds:F2} ms",
            timer.Elapsed.TotalMilliseconds);
        // One atomic graph save; participates in the demo caller's transaction when present.
        timer.Restart();
        try { await context.SaveChangesAsync(cancellationToken); }
        finally
        {
            logger.LogInformation("[Authentication Performance] SaveChanges: {ElapsedMilliseconds:F2} ms; checkpoints: 1",
                timer.Elapsed.TotalMilliseconds);
        }
        timer.Restart();
        var access = jwtProvider.GenerateToken(user, roles, session.Id);
        logger.LogInformation("[Authentication Performance] JWT generation: {ElapsedMilliseconds:F2} ms",
            timer.Elapsed.TotalMilliseconds);
        return new AuthenticationResult(access.AccessToken, access.ExpiresIn, plaintext, rememberMe, absolute);
    }
}
