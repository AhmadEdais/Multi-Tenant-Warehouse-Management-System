namespace WMS.Application.Common.Interfaces;

public sealed record TenantProvisioningDetails(string Code, string Name, string AdminFullName,
    string AdminEmail, string InitialPassword);

public sealed record ProvisionedTenant(Tenant Tenant, User AdminUser);

public interface ITenantProvisioningService
{
    Task<ProvisionedTenant> CreateNormalAsync(TenantProvisioningDetails details, int systemAdminId,
        CancellationToken cancellationToken);
    Task<ProvisionedTenant> CreateDemoAsync(CancellationToken cancellationToken);
}
