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
}
