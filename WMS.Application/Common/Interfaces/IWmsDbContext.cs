namespace WMS.Application.Common.Interfaces;

public interface IWmsDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Warehouse> Warehouses { get; }
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}