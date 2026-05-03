using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
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
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WmsDbContext).Assembly);
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IMustBelongToTenant).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(ConvertFilterExpression<IMustBelongToTenant>(
                        e => e.TenantId == _tenantContext.TenantId || _tenantContext.IsSystemRequest,
                        entityType.ClrType));
            }
        }
         modelBuilder.Entity<User>()
            .HasQueryFilter(u => _tenantContext.IsSystemRequest || u.TenantId == _tenantContext.TenantId); // doesn't belong to IMustBelongToTenant, but we want to filter it as well
    }
    private static LambdaExpression ConvertFilterExpression<TInterface>(
    Expression<Func<TInterface, bool>> filterExpression, Type entityType)
    {
        var newParam = Expression.Parameter(entityType);
        var newBody = ReplacingExpressionVisitor.Replace(filterExpression.Parameters.Single(), newParam, filterExpression.Body);
        return Expression.Lambda(newBody, newParam);
    }
}