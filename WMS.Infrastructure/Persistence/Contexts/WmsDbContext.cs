using WMS.Application.Common.Interfaces;

namespace WMS.Infrastructure.Persistence.Contexts;

public class WmsDbContext(DbContextOptions<WmsDbContext> options, ITenantContext tenantContext) : DbContext(options) , IWmsDbContext
{
    private readonly ITenantContext _tenantContext = tenantContext;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole > UserRoles => Set<UserRole>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WmsDbContext).Assembly);

        var tenantId = _tenantContext.TenantId;
        var isSystemRequest = _tenantContext.IsSystemRequest;

         modelBuilder.Entity<Warehouse>()
            .HasQueryFilter(w => _tenantContext.IsSystemRequest || w.TenantId == _tenantContext.TenantId);

         modelBuilder.Entity<User>()
            .HasQueryFilter(u => _tenantContext.IsSystemRequest || u.TenantId == _tenantContext.TenantId);;
    }
}