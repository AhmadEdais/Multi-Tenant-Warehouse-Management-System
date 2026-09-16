using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities;

public class Supplier : IMustBelongToTenant
{
    public int Id { get; private set; }
    public int TenantId { get; set; } 
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? ContactEmail { get; private set; }
    public string PhoneNumber { get; private set; } = string.Empty;
    public string? Address { get; private set; }
    public bool IsActive { get; private set; }

    private Supplier() { }

    public static Supplier Create(
        string code,
        string name,
        string? contactEmail,
        string phoneNumber,
        string? address)
    {
        return new Supplier
        {
            Code = code,
            Name = name,
            ContactEmail = contactEmail,
            PhoneNumber = phoneNumber,
            Address = address,
            IsActive = true
        };
    }

    public void Update(
        string name,
        string? contactEmail,
        string phoneNumber,
        string? address)
    {
        Name = name;
        ContactEmail = contactEmail;
        PhoneNumber = phoneNumber;
        Address = address;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}