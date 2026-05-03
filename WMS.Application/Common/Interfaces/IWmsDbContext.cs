using Microsoft.EntityFrameworkCore.Infrastructure;

namespace WMS.Application.Common.Interfaces;

public interface IWmsDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Warehouse> Warehouses { get; }
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductCategory> ProductCategories { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}