namespace WMS.Infrastructure.Persistence.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

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