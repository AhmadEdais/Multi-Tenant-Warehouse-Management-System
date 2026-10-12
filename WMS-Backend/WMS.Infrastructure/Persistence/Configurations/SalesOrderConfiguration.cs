using WMS.Domain.Enums;

namespace WMS.Infrastructure.Persistence.Configurations;

internal sealed class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.ToTable("SalesOrders", table =>
            table.HasCheckConstraint("CK_SalesOrders_Status", "[Status] IN (1, 2, 3, 4)"));
        builder.HasKey(order => order.Id).HasName("PK_SalesOrders");
        builder.Property(order => order.Id).ValueGeneratedOnAdd();
        builder.Property(order => order.TenantId).IsRequired();
        builder.Property(order => order.CustomerId).IsRequired();
        builder.Property(order => order.WarehouseId).IsRequired();
        builder.Property(order => order.OrderNumber).HasMaxLength(50).IsRequired();
        builder.Property(order => order.Status).HasColumnType("tinyint")
            .HasDefaultValue(SalesOrderStatus.Draft).IsRequired();
        builder.Property(order => order.RequestedShipmentDate).HasColumnType("date");
        builder.Property(order => order.RowVersion).IsRowVersion().IsRequired();
        builder.Property(order => order.CreatedOnUtc).HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()").ValueGeneratedOnAdd().IsRequired();
        builder.Property(order => order.CreatedBy).HasMaxLength(128).IsRequired();
        builder.Property(order => order.LastModifiedOnUtc).HasColumnType("datetime2");
        builder.Property(order => order.LastModifiedBy).HasMaxLength(128);

        builder.HasOne<Tenant>().WithMany().HasForeignKey(order => order.TenantId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesOrders_Tenants");
        builder.HasOne<Customer>().WithMany().HasForeignKey(order => order.CustomerId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesOrders_Customers");
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(order => order.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesOrders_Warehouses");
        builder.HasIndex(order => new { order.TenantId, order.OrderNumber }).IsUnique()
            .HasDatabaseName("UQ_SalesOrders_Tenant_OrderNumber");
        builder.HasIndex(order => new { order.CustomerId, order.Status })
            .HasDatabaseName("IX_SalesOrders_Customer_Status");

        builder.HasMany(order => order.Lines).WithOne().HasForeignKey(line => line.SalesOrderId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_SalesOrderLines_SalesOrders");
        builder.Metadata.FindNavigation(nameof(SalesOrder.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
