namespace WMS.Domain.Interfaces;

public interface IMustBelongToTenant
{
    int TenantId { get; set; }
}