namespace WMS.Application.Common.Interfaces;

public interface ITenantContext
{
    int TenantId { get; }
    bool IsSystemRequest { get; }
}