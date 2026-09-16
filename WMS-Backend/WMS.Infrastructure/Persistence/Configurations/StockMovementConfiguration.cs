namespace WMS.Infrastructure.Persistence.Configurations;

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.TenantId).IsRequired();
        builder.Property(m => m.ProductId).IsRequired();
        builder.Property(m => m.LocationId).IsRequired();

        builder.Property(m => m.Quantity)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(m => m.MovementType)
            .HasColumnType("tinyint")
            .IsRequired();

        builder.Property(m => m.ReferenceTable)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(m => m.ReferenceId)
            .IsRequired(false);

    }
}