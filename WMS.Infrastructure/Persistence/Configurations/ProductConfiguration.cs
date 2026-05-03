namespace WMS.Infrastructure.Persistence.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.SKU).HasMaxLength(100).IsRequired();
            builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
            builder.Property(p => p.Description).HasMaxLength(1000);
            builder.Property(p => p.UnitOfMeasure).HasMaxLength(50).IsRequired();

            // Crucial: Match the SQL decimal precision exactly
            builder.Property(p => p.UnitCost).HasColumnType("decimal(18,4)");
            builder.Property(p => p.UnitPrice).HasColumnType("decimal(18,4)");

            // Concurrency Token
            builder.Property(p => p.RowVersion)
                .IsRowVersion()
                .IsRequired();

            // Indexes & Composite Unique Constraint
            builder.HasIndex(p => new { p.TenantId, p.SKU })
                .IsUnique()
                .HasDatabaseName("UQ_Products_TenantId_SKU");

            builder.HasIndex(p => p.TenantId).HasDatabaseName("IX_Products_TenantId");
        }
    }
}