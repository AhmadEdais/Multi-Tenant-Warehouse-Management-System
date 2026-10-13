using WMS.Domain.Enums;

namespace WMS.Infrastructure.Persistence.Configurations;

internal sealed class SalesOrderAllocationConfiguration : IEntityTypeConfiguration<SalesOrderAllocation>
{
    public void Configure(EntityTypeBuilder<SalesOrderAllocation> builder)
    {
        builder.ToTable("SalesOrderAllocations", table =>
        {
            table.HasCheckConstraint("CHK_SalesOrderAllocations_Quantity", "[Quantity] > 0");
            table.HasCheckConstraint("CHK_SalesOrderAllocations_Status", "[Status] IN (1, 2, 3)");
            table.HasCheckConstraint("CHK_SalesOrderAllocations_ReleaseMetadata",
                "([Status] = 1 AND [ReleasedOnUtc] IS NULL AND [ReleasedBy] IS NULL AND [ReleaseReason] IS NULL) " +
                "OR ([Status] = 2 AND [ReleasedOnUtc] IS NOT NULL " +
                "AND [ReleasedBy] IS NOT NULL AND LEN(LTRIM(RTRIM([ReleasedBy]))) > 0 " +
                "AND [ReleaseReason] IS NOT NULL AND LEN(LTRIM(RTRIM([ReleaseReason]))) > 0) " +
                "OR [Status] = 3");
        });
        builder.HasKey(allocation => allocation.Id).HasName("PK_SalesOrderAllocations");
        builder.Property(allocation => allocation.Id).ValueGeneratedOnAdd();
        builder.Property(allocation => allocation.TenantId).IsRequired();
        builder.Property(allocation => allocation.SalesOrderLineId).IsRequired();
        builder.Property(allocation => allocation.StockLevelId).IsRequired();
        builder.Property(allocation => allocation.Quantity).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(allocation => allocation.Status).HasColumnType("tinyint")
            .HasDefaultValue(SalesOrderAllocationStatus.Active).IsRequired();
        builder.Property(allocation => allocation.CreatedOnUtc).HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()").IsRequired();
        builder.Property(allocation => allocation.CreatedBy).HasMaxLength(128).IsRequired();
        builder.Property(allocation => allocation.ReleasedOnUtc).HasColumnType("datetime2");
        builder.Property(allocation => allocation.ReleasedBy).HasMaxLength(128);
        builder.Property(allocation => allocation.ReleaseReason).HasMaxLength(200);

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
