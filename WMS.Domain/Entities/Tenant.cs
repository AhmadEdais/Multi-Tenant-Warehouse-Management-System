namespace WMS.Domain.Entities;

public  class Tenant
{
    public int Id { get;private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
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
}
