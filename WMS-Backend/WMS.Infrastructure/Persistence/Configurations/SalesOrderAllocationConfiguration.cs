namespace WMS.Infrastructure.Persistence.Configurations;

internal sealed class SalesOrderAllocationConfiguration : IEntityTypeConfiguration<SalesOrderAllocation>
{
    public void Configure(EntityTypeBuilder<SalesOrderAllocation> builder)
    {
        builder.ToTable("SalesOrderAllocations", table =>
            table.HasCheckConstraint("CHK_SalesOrderAllocations_Quantity", "[Quantity] > 0"));
        builder.HasKey(allocation => allocation.Id).HasName("PK_SalesOrderAllocations");
        builder.Property(allocation => allocation.Id).ValueGeneratedOnAdd();
        builder.Property(allocation => allocation.TenantId).IsRequired();
        builder.Property(allocation => allocation.SalesOrderLineId).IsRequired();
        builder.Property(allocation => allocation.StockLevelId).IsRequired();
        builder.Property(allocation => allocation.Quantity).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(allocation => allocation.CreatedOnUtc).HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()").IsRequired();
        builder.Property(allocation => allocation.CreatedBy).HasMaxLength(128).IsRequired();

        builder.HasOne<Tenant>().WithMany().HasForeignKey(allocation => allocation.TenantId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesOrderAllocations_Tenants");
        builder.HasOne<StockLevel>().WithMany().HasForeignKey(allocation => allocation.StockLevelId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_SalesOrderAllocations_StockLevels");
        builder.HasIndex(allocation => new { allocation.SalesOrderLineId, allocation.StockLevelId })
            .IsUnique().HasDatabaseName("UQ_SalesOrderAllocations_Line_StockLevel");
        builder.HasIndex(allocation => allocation.StockLevelId)
            .HasDatabaseName("IX_SalesOrderAllocations_StockLevelId");
    }
}
