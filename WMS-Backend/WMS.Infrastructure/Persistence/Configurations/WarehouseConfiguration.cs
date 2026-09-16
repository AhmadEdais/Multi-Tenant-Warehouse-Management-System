using Microsoft.EntityFrameworkCore;

namespace WMS.Infrastructure.Persistence.Configurations;

public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");

        builder.HasKey(w => w.Id)
               .HasName("PK_Warehouses");

        builder.Property(w => w.Code)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(w => w.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(w => w.Address)
               .HasMaxLength(500);

        
        builder.Property(w => w.IsActive)
               .IsRequired()
               .HasDefaultValue(true);
        builder.Property(w => w.CreatedAtUtc)
               .IsRequired();
        builder.Property(w => w.RowVersion)
               .IsRowVersion();

        builder.HasOne(w => w.Tenant)
               .WithMany(t => t.Warehouses)
               .HasForeignKey(w => w.TenantId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_Warehouses_Tenants"); 

        builder.HasIndex(w => new { w.TenantId, w.Code })
               .IsUnique()
               .HasDatabaseName("UQ_Warehouses_Tenant_Code") 
               .IsClustered(false); 

        builder.HasQueryFilter(w => w.IsActive);

    }
}