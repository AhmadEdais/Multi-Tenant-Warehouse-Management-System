using WMS.Application.Common.Interfaces;

namespace WMS.Infrastructure.Identity;

public class DevTenantContext : ITenantContext
{
    // DEV-ONLY — replaced in Slice 3 by HttpTenantContext
    public int TenantId => 1; // Hardcoded to demo tenant

    // Set to true so you can test creating tenants via SystemAdmin mode
    public bool IsSystemRequest => true;
}