namespace WMS.Domain.Entities;

public  class Tenant
{
    public int Id { get;private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public bool IsDemo { get; private set; }
    public DateTime? DemoExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public int? CreatedByUserId { get; private set; }
    public virtual ICollection<Warehouse> Warehouses { get; private set; } = [];
    
    private Tenant() { }

    public static Tenant Create(string code, string name, int? createdByUserId)
    {
        return new Tenant
        {
            Code = code,
            Name = name,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };
    }

    public static Tenant CreateDemo(string code, string name, DateTime expiresAtUtc)
    {
        if (expiresAtUtc.Kind != DateTimeKind.Utc || expiresAtUtc <= DateTime.UtcNow)
            throw new ArgumentException("Demo expiration must be in the future and UTC.", nameof(expiresAtUtc));
        var tenant = Create(code, name, null);
        tenant.IsDemo = true;
        tenant.DemoExpiresAtUtc = expiresAtUtc;
        return tenant;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Reactivate()
    {
        IsActive = true;
    }
}
