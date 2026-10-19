namespace WMS.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(token => token.Id).HasName("PK_RefreshTokens");
        builder.Property(token => token.Id).ValueGeneratedOnAdd().UseIdentityColumn(1, 1);
        builder.Property(token => token.AuthSessionId).IsRequired();
        builder.Property(token => token.TokenHash).HasColumnType("binary(32)")
            .HasMaxLength(32).IsFixedLength().IsRequired();
        builder.Property(token => token.CreatedAtUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(token => token.ExpiresAtUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(token => token.ConsumedAtUtc).HasColumnType("datetime2");
        builder.Property(token => token.ReplacedByTokenId);
        builder.Property(token => token.ReuseDetectedAtUtc).HasColumnType("datetime2");
        builder.Property(token => token.RowVersion).IsRowVersion().IsRequired();

        builder.HasOne(token => token.AuthSession).WithMany(session => session.RefreshTokens)
            .HasForeignKey(token => token.AuthSessionId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_RefreshTokens_AuthSessions");
        builder.HasOne(token => token.ReplacedByToken).WithMany()
            .HasForeignKey(token => token.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_RefreshTokens_ReplacementToken");

        builder.HasIndex(token => token.AuthSessionId).HasDatabaseName("IX_RefreshTokens_AuthSessionId");
        builder.HasIndex(token => token.ReplacedByTokenId)
            .HasDatabaseName("IX_RefreshTokens_ReplacedByTokenId");
        builder.HasIndex(token => token.TokenHash).IsUnique()
            .HasDatabaseName("UX_RefreshTokens_TokenHash");
    }
}
