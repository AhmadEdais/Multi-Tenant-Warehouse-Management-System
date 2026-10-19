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
}
