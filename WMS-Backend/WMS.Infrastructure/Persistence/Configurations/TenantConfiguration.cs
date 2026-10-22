namespace WMS.Infrastructure.Persistence.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants", table => table.HasCheckConstraint("CK_Tenants_DemoExpiration",
            "([IsDemo] = 0 AND [DemoExpiresAtUtc] IS NULL) OR " +
            "([IsDemo] = 1 AND [DemoExpiresAtUtc] IS NOT NULL AND [DemoExpiresAtUtc] > [CreatedAtUtc])"));
        builder.Property(t => t.IsDemo).IsRequired().HasDefaultValue(false);
        builder.Property(t => t.DemoExpiresAtUtc).HasColumnType("datetime2");
        builder.HasIndex(t => new { t.IsDemo, t.Id }).HasFilter("[IsDemo] = 1")
            .HasDatabaseName("IX_Tenants_DemoCapacity");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Code)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(t => t.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(t => t.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        builder.Property(t => t.CreatedAtUtc)
               .IsRequired();
        builder.Property(t => t.CreatedByUserId)
       .IsRequired(false);

        builder.HasIndex(t => t.Code)
               .IsUnique();
    }
}
