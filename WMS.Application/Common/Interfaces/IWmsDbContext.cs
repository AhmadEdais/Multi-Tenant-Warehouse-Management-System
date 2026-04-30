namespace WMS.Application.Common.Interfaces;

public interface IWmsDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Warehouse> Warehouses { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}