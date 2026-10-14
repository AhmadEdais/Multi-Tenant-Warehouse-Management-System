namespace WMS.Infrastructure.Persistence.Configurations;

internal sealed class SalesOrderPickupLineConfiguration : IEntityTypeConfiguration<SalesOrderPickupLine>
{
    public void Configure(EntityTypeBuilder<SalesOrderPickupLine> builder)
    {
        builder.ToTable("SalesOrderPickupLines", table =>
            table.HasCheckConstraint("CHK_SalesOrderPickupLines_Quantity", "[Quantity] > 0"));
        builder.HasKey(line => line.Id).HasName("PK_SalesOrderPickupLines");
        builder.Property(line => line.Id).ValueGeneratedOnAdd();
        builder.Property(line => line.TenantId).IsRequired();
        builder.Property(line => line.SalesOrderPickupId).IsRequired();
        builder.Property(line => line.SalesOrderAllocationId).IsRequired();
        builder.Property(line => line.Quantity).HasColumnType("decimal(18,2)").IsRequired();

        builder.HasOne<Tenant>().WithMany().HasForeignKey(line => line.TenantId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesOrderPickupLines_Tenants");
        builder.HasOne<SalesOrderAllocation>().WithMany()
            .HasForeignKey(line => line.SalesOrderAllocationId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesOrderPickupLines_Allocations");

        builder.HasIndex(line => new { line.SalesOrderPickupId, line.SalesOrderAllocationId })
            .IsUnique().HasDatabaseName("UQ_SalesOrderPickupLines_Pickup_Allocation");
        builder.HasIndex(line => line.SalesOrderAllocationId)
            .HasDatabaseName("IX_SalesOrderPickupLines_AllocationId");
    }
}
