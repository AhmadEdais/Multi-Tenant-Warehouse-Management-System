namespace WMS.Infrastructure.Persistence.Configurations;

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ContactEmail).HasMaxLength(256);

        builder.Property(x => x.PhoneNumber).HasMaxLength(50).IsRequired();

        builder.Property(x => x.Address).HasMaxLength(500);

        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();

        builder.HasQueryFilter(s => s.IsActive);
    }
}