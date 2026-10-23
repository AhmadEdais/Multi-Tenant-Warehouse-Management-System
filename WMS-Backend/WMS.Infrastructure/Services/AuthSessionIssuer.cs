namespace WMS.Infrastructure.Services;

internal sealed class AuthSessionIssuer(IWmsDbContext context, IJwtProvider jwtProvider,
    IRefreshTokenGenerator refreshTokenGenerator, IOptions<AuthSessionPolicy> policy) : IAuthSessionIssuer
{
    public async Task<AuthenticationResult> IssueAsync(User user, IReadOnlyList<string> roles,
        bool rememberMe, DateTime? tenantDeadlineUtc, CancellationToken cancellationToken)
    {
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
        // One atomic graph save; participates in the demo caller's transaction when present.
        await context.SaveChangesAsync(cancellationToken);
        var access = jwtProvider.GenerateToken(user, roles, session.Id);
        return new AuthenticationResult(access.AccessToken, access.ExpiresIn, plaintext, rememberMe, absolute);
    }
}
