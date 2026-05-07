namespace WMS.Infrastructure.Persistence.Configurations;

internal sealed class StockLevelConfiguration : IEntityTypeConfiguration<StockLevel>
{
    public void Configure(EntityTypeBuilder<StockLevel> builder)
    {
        builder.ToTable("StockLevels", t =>
        {
            t.HasCheckConstraint("CHK_StockLevels_QuantityOnHand", "[QuantityOnHand] >= 0");
            t.HasCheckConstraint("CHK_StockLevels_QuantityAllocated", "[QuantityAllocated] >= 0");
        });

        builder.HasKey(s => s.Id);

        builder.Property(s => s.TenantId).IsRequired();
        builder.Property(s => s.ProductId).IsRequired();
        builder.Property(s => s.LocationId).IsRequired();

        builder.Property(s => s.QuantityOnHand)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(s => s.QuantityAllocated)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(s => s.RowVersion)
            .IsRowVersion()
            .IsRequired();

        builder.HasIndex(s => new { s.TenantId, s.ProductId, s.LocationId })
            .IsUnique()
            .HasDatabaseName("UQ_StockLevels_Tenant_Product_Location");

    }
}