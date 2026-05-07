namespace WMS.Infrastructure.Persistence.Configurations;

internal sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("PurchaseOrderLines", t =>
        {
            t.HasCheckConstraint("CHK_POLines_ExpectedQuantity", "[ExpectedQuantity] > 0");
            t.HasCheckConstraint("CHK_POLines_ReceivedQuantity", "[ReceivedQuantity] >= 0 AND [ReceivedQuantity] <= [ExpectedQuantity]");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ExpectedQuantity).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.ReceivedQuantity).HasColumnType("decimal(18,2)").IsRequired();

        builder.HasIndex(x => new { x.PurchaseOrderId, x.ProductId })
            .IsUnique()
            .HasDatabaseName("UQ_PurchaseOrderLines_PO_Product");
    }
}