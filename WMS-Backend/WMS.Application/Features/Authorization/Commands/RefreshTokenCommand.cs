namespace WMS.Application.Features.Authorization.Commands;

public sealed record RefreshTokenCommand(string? Token) : IRequest<AuthenticationResult>;

internal sealed class RefreshTokenCommandHandler(
    IWmsDbContext context,
    IRefreshTokenGenerator refreshTokenGenerator,
    IJwtProvider jwtProvider,
    Microsoft.Extensions.Options.IOptions<AuthSessionPolicy> sessionPolicy)
    : IRequestHandler<RefreshTokenCommand, AuthenticationResult>
{
    // A second use immediately after rotation may be a competing browser request.
    // It gets no credentials but does not automatically revoke the session.
    private static readonly TimeSpan RecentRotationWindow = TimeSpan.FromSeconds(5);
    private const string InvalidCredentialMessage = "Invalid refresh credentials.";

    public async Task<AuthenticationResult> Handle(
        RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (!refreshTokenGenerator.TryComputeHash(request.Token, out var tokenHash))
            throw new UnauthorizedAccessException(InvalidCredentialMessage);

        var token = await context.RefreshTokens
            .Include(existing => existing.AuthSession)
            .FirstOrDefaultAsync(existing => existing.TokenHash == tokenHash, cancellationToken)
            ?? throw new UnauthorizedAccessException(InvalidCredentialMessage);

        var nowUtc = DateTime.UtcNow;
        var session = token.AuthSession;
        if (session.RevokedAtUtc.HasValue)
            throw new UnauthorizedAccessException(InvalidCredentialMessage);

        if (token.ConsumedAtUtc.HasValue)
        {
            var elapsedSinceRotation = nowUtc - token.ConsumedAtUtc.Value;
            // A small negative interval tolerates clock skew between API instances.
            if (elapsedSinceRotation >= -RecentRotationWindow &&
                elapsedSinceRotation <= RecentRotationWindow)
                throw new ConflictException("Refresh was already completed by another request.");

            token.RecordReuse(nowUtc);
            session.RevokeForRefreshTokenReuse(nowUtc);
            await context.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException(InvalidCredentialMessage);
        }

        if (token.ExpiresAtUtc <= nowUtc || session.IdleExpiresAtUtc <= nowUtc ||
            session.AbsoluteExpiresAtUtc <= nowUtc)
            throw new UnauthorizedAccessException(InvalidCredentialMessage);


        var user = await context.Users.IgnoreQueryFilters()
            .Include(existing => existing.UserRoles)
                .ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(existing => existing.Id == session.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException(InvalidCredentialMessage);

        var roleNames = user.UserRoles.Select(userRole => userRole.Role.Name).ToList();
        if (roleNames.Count == 0 ||
            (user.TenantId.HasValue && roleNames.Any(role => !Roles.TenantAssignable.Contains(role))) ||
            (!user.TenantId.HasValue && roleNames.Any(role => role != Roles.SystemAdmin)))
            throw new UnauthorizedAccessException(InvalidCredentialMessage);

        if (user.TenantId.HasValue && !await context.Tenants.AnyAsync(
                tenant => tenant.Id == user.TenantId.Value && tenant.IsActive,
                cancellationToken))
            throw new UnauthorizedAccessException(InvalidCredentialMessage);

        var updatedIdleDeadline = sessionPolicy.Value.GetRefreshedIdleDeadline(
            nowUtc, session.RememberMe, session.AbsoluteExpiresAtUtc);
        var (replacementPlaintext, replacementHash) = refreshTokenGenerator.Generate();
        var accessToken = jwtProvider.GenerateToken(user, roleNames, session.Id);

        var replacement = session.AddReplacementRefreshToken(
            replacementHash, nowUtc, updatedIdleDeadline);
        token.Consume(replacement, nowUtc);
        session.RecordRefresh(nowUtc, updatedIdleDeadline);

        await context.SaveChangesAsync(cancellationToken);

        return new AuthenticationResult(accessToken.AccessToken, accessToken.ExpiresIn,
            replacementPlaintext, session.RememberMe, session.AbsoluteExpiresAtUtc);
    }
}
