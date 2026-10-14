namespace WMS.Infrastructure.Persistence.Configurations;

internal sealed class SalesOrderPickupConfiguration : IEntityTypeConfiguration<SalesOrderPickup>
{
    public void Configure(EntityTypeBuilder<SalesOrderPickup> builder)
    {
        builder.ToTable("SalesOrderPickups");
        builder.HasKey(pickup => pickup.Id).HasName("PK_SalesOrderPickups");
        builder.Property(pickup => pickup.Id).ValueGeneratedOnAdd();
        builder.Property(pickup => pickup.TenantId).IsRequired();
        builder.Property(pickup => pickup.SalesOrderId).IsRequired();
        builder.Property(pickup => pickup.PickedOnUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(pickup => pickup.PickedBy).HasMaxLength(128).IsRequired();
        builder.Property(pickup => pickup.ReceivedBy).HasMaxLength(200);
        builder.Property(pickup => pickup.Notes).HasMaxLength(500);

        builder.HasOne<Tenant>().WithMany().HasForeignKey(pickup => pickup.TenantId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesOrderPickups_Tenants");
        builder.HasOne<SalesOrder>().WithMany().HasForeignKey(pickup => pickup.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesOrderPickups_SalesOrders");
        builder.HasMany(pickup => pickup.Lines).WithOne()
            .HasForeignKey(line => line.SalesOrderPickupId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesOrderPickupLines_Pickups");
        builder.Metadata.FindNavigation(nameof(SalesOrderPickup.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(pickup => new { pickup.SalesOrderId, pickup.PickedOnUtc, pickup.Id })
            .IsDescending(false, true, true)
            .HasDatabaseName("IX_SalesOrderPickups_SalesOrder_PickedOn");
    }
}
