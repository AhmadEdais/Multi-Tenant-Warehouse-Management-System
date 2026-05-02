namespace WMS.Infrastructure.Services;

internal sealed class HttpTenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public int? TenantId
    {
        get
        {
            var tenantClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("tenantId");

            if (tenantClaim != null && int.TryParse(tenantClaim.Value, out int tenantId))
            {
                return tenantId;
            }

            return null; 
        }
    }

    public bool IsSystemRequest =>
         (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true) &&
         !TenantId.HasValue;
}