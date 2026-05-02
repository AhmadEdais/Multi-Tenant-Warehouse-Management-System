namespace WMS.Infrastructure.Persistence.Interceptors;

public sealed class TenantStampingInterceptor(ITenantContext tenantContext) : SaveChangesInterceptor
{
    private readonly ITenantContext _tenantContext = tenantContext;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        StampTenantId(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        StampTenantId(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void StampTenantId(DbContext? context)
    {
        if (context == null) return;

        // Find all entities being ADDED to the database that require a TenantId
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added && e.Entity is IMustBelongToTenant);

        foreach (var entry in entries)
        {
            // If the developer forgot to set the TenantId, or tried to set it maliciously,
            // we ruthlessly override it with the actual cryptographic claim from the JWT.
            if (_tenantContext.TenantId.HasValue)
            {
                ((IMustBelongToTenant)entry.Entity).TenantId = _tenantContext.TenantId.Value;
            }
        }
    }
}