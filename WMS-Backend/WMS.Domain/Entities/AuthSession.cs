namespace WMS.Domain.Entities;

public sealed class AuthSession
{
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? LastRefreshedAtUtc { get; private set; }
    public DateTime IdleExpiresAtUtc { get; private set; }
    public DateTime AbsoluteExpiresAtUtc { get; private set; }
    public bool RememberMe { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string? RevocationReason { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public User User { get; private set; } = null!;

    private readonly List<RefreshToken> _refreshTokens = [];
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private AuthSession() { }

    public static AuthSession Create(int userId, DateTime nowUtc, DateTime idleExpiresAtUtc,
        DateTime absoluteExpiresAtUtc, bool rememberMe)
    {
        if (userId <= 0 || idleExpiresAtUtc <= nowUtc || absoluteExpiresAtUtc < idleExpiresAtUtc)
            throw new ArgumentException("Invalid authentication session details.");

        return new AuthSession
        {
            UserId = userId,
            CreatedAtUtc = nowUtc,
            IdleExpiresAtUtc = idleExpiresAtUtc,
            AbsoluteExpiresAtUtc = absoluteExpiresAtUtc,
            RememberMe = rememberMe
        };
    }

    public void AddInitialRefreshToken(byte[] tokenHash, DateTime expiresAtUtc)
    {
        if (_refreshTokens.Count != 0 || expiresAtUtc > IdleExpiresAtUtc || expiresAtUtc > AbsoluteExpiresAtUtc)
            throw new InvalidOperationException("Invalid initial refresh token expiration.");

        _refreshTokens.Add(RefreshToken.Create(this, tokenHash, CreatedAtUtc, expiresAtUtc));
    }
}
