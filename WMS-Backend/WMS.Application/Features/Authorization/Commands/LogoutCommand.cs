namespace WMS.Application.Features.Authorization.Commands;

public sealed record LogoutCommand(string? Token) : IRequest;

internal sealed class LogoutCommandHandler(
    IWmsDbContext context,
    IRefreshTokenGenerator refreshTokenGenerator) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (!refreshTokenGenerator.TryComputeHash(request.Token, out var tokenHash))
            return;

        var token = await context.RefreshTokens
            .Include(existing => existing.AuthSession)
            .FirstOrDefaultAsync(existing => existing.TokenHash == tokenHash, cancellationToken);
        if (token is null)
            return;

        var nowUtc = DateTime.UtcNow;
        var session = token.AuthSession;
        if (session.RevokedAtUtc.HasValue ||
            session.IdleExpiresAtUtc <= nowUtc || session.AbsoluteExpiresAtUtc <= nowUtc)
            return;

        if (token.ConsumedAtUtc.HasValue)
        {
            // A refresh may have consumed this browser's cookie moments before
            // logout. Only this narrow race can use a consumed token to revoke.
            var elapsedSinceRotation = nowUtc - token.ConsumedAtUtc.Value;
            if (elapsedSinceRotation < -AuthSessionPolicy.RecentRotationWindow ||
                elapsedSinceRotation > AuthSessionPolicy.RecentRotationWindow)
                return;
        }
        else if (token.ExpiresAtUtc <= nowUtc)
        {
            return;
        }

        session.RevokeForLogout(nowUtc);
        await context.SaveChangesAsync(cancellationToken);
    }
}
