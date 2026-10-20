namespace WMS.Domain.Entities;

public sealed class RefreshToken
{
    public int Id { get; private set; }
    public int AuthSessionId { get; private set; }
    public byte[] TokenHash { get; private set; } = [];
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? ConsumedAtUtc { get; private set; }
    public int? ReplacedByTokenId { get; private set; }
    public DateTime? ReuseDetectedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public AuthSession AuthSession { get; private set; } = null!;
    public RefreshToken? ReplacedByToken { get; private set; }

    private RefreshToken() { }

    internal static RefreshToken Create(AuthSession session, byte[] tokenHash,
        DateTime createdAtUtc, DateTime expiresAtUtc)
    {
        if (tokenHash.Length != 32 || expiresAtUtc <= createdAtUtc)
            throw new ArgumentException("Invalid refresh token details.");

        return new RefreshToken
        {
            AuthSession = session,
            TokenHash = (byte[])tokenHash.Clone(),
            CreatedAtUtc = createdAtUtc,
            ExpiresAtUtc = expiresAtUtc
        };
    }
}
