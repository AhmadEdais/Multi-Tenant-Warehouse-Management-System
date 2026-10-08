namespace WMS.Infrastructure.Persistence.Configurations;

internal sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");

        builder.HasKey(x => x.Id).HasName("PK_PurchaseOrders");

        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.SupplierId).IsRequired();
        builder.Property(x => x.WarehouseId).IsRequired();

        builder.Property(x => x.OrderNumber).HasMaxLength(50).IsRequired();

        builder.Property(x => x.Status)
            .HasColumnType("tinyint")
            .HasDefaultValue(WMS.Domain.Enums.PurchaseOrderStatus.Draft)
            .IsRequired();

        builder.Property(x => x.ExpectedDeliveryDate).HasColumnType("date");
        builder.Property(x => x.RowVersion).IsRowVersion().IsRequired();
        builder.Property(x => x.CreatedOnUtc)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()")
            .ValueGeneratedOnAdd()
            .IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(128).IsRequired();
        builder.Property(x => x.LastModifiedOnUtc).HasColumnType("datetime2");
        builder.Property(x => x.LastModifiedBy).HasMaxLength(128);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_PurchaseOrders_Tenants");

        builder.HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_PurchaseOrders_Suppliers");

        builder.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_PurchaseOrders_Warehouses");

        builder.HasIndex(x => new { x.TenantId, x.OrderNumber })
            .IsUnique()
            .HasDatabaseName("UQ_PurchaseOrders_Tenant_OrderNumber");

        builder.HasIndex(x => new { x.SupplierId, x.Status })
            .HasDatabaseName("IX_PurchaseOrders_Supplier_Status");

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_PurchaseOrderLines_PurchaseOrders");

        builder.Metadata.FindNavigation(nameof(PurchaseOrder.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
