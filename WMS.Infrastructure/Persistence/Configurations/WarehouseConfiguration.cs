namespace WMS.Infrastructure.Persistence.Configurations;

public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        // 1. Map to the specific table
        builder.ToTable("Warehouses");

        // 2. Configure the Primary Key WITH Constraint Name
        builder.HasKey(w => w.Id)
               .HasName("PK_Warehouses");

        // 3. Configure the Columns
        builder.Property(w => w.Code)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(w => w.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(w => w.Address)
               .HasMaxLength(500);

        // Note: EF Core doesn't need the name of the Default Constraint 
        // (DF_Warehouses_IsActive) to insert data correctly, just the value.
        builder.Property(w => w.IsActive)
               .IsRequired()
               .HasDefaultValue(true);
        builder.Property(w => w.CreatedAtUtc)
               .IsRequired();
        // THE CONCURRENCY MAGIC
        builder.Property(w => w.RowVersion)
               .IsRowVersion();

        // 4. Configure the Relationship (Foreign Key) WITH Constraint Name
        builder.HasOne(w => w.Tenant)
               .WithMany(t => t.Warehouses)
               .HasForeignKey(w => w.TenantId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_Warehouses_Tenants"); // <-- Added this

        // 5. Configure the Composite Unique Constraint WITH Constraint Name
        builder.HasIndex(w => new { w.TenantId, w.Code })
               .IsUnique()
               .HasDatabaseName("UQ_Warehouses_Tenant_Code") // <-- Added this
               .IsClustered(false); // Optional: EF Core assumes false for unique indexes

        builder.HasQueryFilter(w => w.IsActive);
    }
}