namespace WMS.Infrastructure.Persistence.Configurations;

internal sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderNumber).HasMaxLength(50).IsRequired();

        builder.Property(x => x.Status).HasColumnType("tinyint").IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.OrderNumber })
            .IsUnique()
            .HasDatabaseName("UQ_PurchaseOrders_Tenant_OrderNumber");

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(PurchaseOrder.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}