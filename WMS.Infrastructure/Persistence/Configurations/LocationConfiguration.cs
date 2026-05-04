namespace WMS.Infrastructure.Persistence.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.LocationType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.Barcode)
            .HasMaxLength(100);

        builder.Property(l => l.MaxWeightCapacityKg)
            .HasColumnType("decimal(18,2)");

        builder.HasOne(l => l.Warehouse)
            .WithMany() 
            .HasForeignKey(l => l.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.ParentLocation)
            .WithMany(l => l.ChildLocations)
            .HasForeignKey(l => l.ParentLocationId)
            .OnDelete(DeleteBehavior.Restrict); 

        
        builder.HasIndex(l => new { l.WarehouseId, l.Barcode })
            .IsUnique()
            .HasDatabaseName("UQ_Locations_WarehouseId_Barcode")
            .HasFilter("[Barcode] IS NOT NULL");
        builder.HasQueryFilter(x => x.IsActive);
    }
}