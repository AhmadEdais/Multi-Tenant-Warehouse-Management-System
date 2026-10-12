namespace WMS.Infrastructure.Persistence.Configurations;

internal sealed class SalesOrderLineConfiguration : IEntityTypeConfiguration<SalesOrderLine>
{
    public void Configure(EntityTypeBuilder<SalesOrderLine> builder)
    {
        builder.ToTable("SalesOrderLines", table =>
        {
            table.HasCheckConstraint("CHK_SalesOrderLines_Quantity", "[Quantity] > 0");
            table.HasCheckConstraint("CHK_SalesOrderLines_QuantityAllocated", "[QuantityAllocated] >= 0 AND [QuantityAllocated] <= [Quantity]");
            table.HasCheckConstraint("CHK_SalesOrderLines_UnitPrice", "[UnitPrice] >= 0");
        });
        builder.HasKey(line => line.Id).HasName("PK_SalesOrderLines");
        builder.Property(line => line.Id).ValueGeneratedOnAdd();
        builder.Property(line => line.TenantId).IsRequired();
        builder.Property(line => line.SalesOrderId).IsRequired();
        builder.Property(line => line.ProductId).IsRequired();
        builder.Property(line => line.Quantity).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(line => line.QuantityAllocated).HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m).IsRequired();
        builder.Property(line => line.UnitPrice).HasColumnType("decimal(18,4)").IsRequired();

        builder.HasOne<Tenant>().WithMany().HasForeignKey(line => line.TenantId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesOrderLines_Tenants");
        builder.HasOne<Product>().WithMany().HasForeignKey(line => line.ProductId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesOrderLines_Products");
        builder.HasIndex(line => new { line.SalesOrderId, line.ProductId }).IsUnique()
            .HasDatabaseName("UQ_SalesOrderLines_SO_Product");

        builder.HasMany(line => line.Allocations).WithOne()
            .HasForeignKey(allocation => allocation.SalesOrderLineId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesOrderAllocations_Lines");
        builder.Metadata.FindNavigation(nameof(SalesOrderLine.Allocations))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
