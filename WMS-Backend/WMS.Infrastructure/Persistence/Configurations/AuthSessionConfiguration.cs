namespace WMS.Infrastructure.Persistence.Configurations;

internal sealed class AuthSessionConfiguration : IEntityTypeConfiguration<AuthSession>
{
    public void Configure(EntityTypeBuilder<AuthSession> builder)
    {
        builder.ToTable("AuthSessions");
        builder.HasKey(session => session.Id).HasName("PK_AuthSessions");
        builder.Property(session => session.Id).ValueGeneratedOnAdd().UseIdentityColumn(1, 1);
        builder.Property(session => session.UserId).IsRequired();
        builder.Property(session => session.CreatedAtUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(session => session.LastRefreshedAtUtc).HasColumnType("datetime2");
        builder.Property(session => session.IdleExpiresAtUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(session => session.AbsoluteExpiresAtUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(session => session.RememberMe).IsRequired();
        builder.Property(session => session.RevokedAtUtc).HasColumnType("datetime2");
        builder.Property(session => session.RevocationReason).HasMaxLength(100);
        builder.Property(session => session.RowVersion).IsRowVersion().IsRequired();

        builder.HasOne(session => session.User).WithMany()
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_AuthSessions_Users");
        builder.Metadata.FindNavigation(nameof(AuthSession.RefreshTokens))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(session => session.UserId).HasDatabaseName("IX_AuthSessions_UserId");
    }
}
