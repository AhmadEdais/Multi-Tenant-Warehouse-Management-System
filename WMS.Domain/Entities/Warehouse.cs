using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities;

public class Warehouse : IMustBelongToTenant
{
    public int Id { get; private set; }
    public int TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Address { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public virtual Tenant? Tenant { get; private set; }

    private Warehouse() { }

    public static Warehouse Create(int tenantId, string code, string name, string? address)
    {
        return new Warehouse
        {
            TenantId = tenantId,
            Code = code,
            Name = name,
            Address = address,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow 
        };
    }
    public void Update(string name, string? address)
    {
        Name = name;
        Address = address;
    }
    public void Deactivate()
    {
       
        IsActive = false;
    }
}