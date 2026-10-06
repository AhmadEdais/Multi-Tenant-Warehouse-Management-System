namespace WMS.Infrastructure.Persistence.Configurations;

internal sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("PurchaseOrderLines", t =>
        {
            t.HasCheckConstraint("CHK_POLines_ExpectedQuantity", "[ExpectedQuantity] > 0");
            t.HasCheckConstraint("CHK_POLines_ReceivedQuantity", "[ReceivedQuantity] >= 0 AND [ReceivedQuantity] <= [ExpectedQuantity]");
            t.HasCheckConstraint("CHK_POLines_UnitCost", "[UnitCost] >= 0");
        });

        builder.HasKey(x => x.Id).HasName("PK_PurchaseOrderLines");

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.PurchaseOrderId).IsRequired();
        builder.Property(x => x.ProductId).IsRequired();

        builder.Property(x => x.ExpectedQuantity).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.ReceivedQuantity)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m)
            .IsRequired();
        builder.Property(x => x.UnitCost).HasColumnType("decimal(18,2)").IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_PurchaseOrderLines_Tenants");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_PurchaseOrderLines_Products");

        builder.HasIndex(x => new { x.PurchaseOrderId, x.ProductId })
            .IsUnique()
            .HasDatabaseName("UQ_PurchaseOrderLines_PO_Product");
    }
}
