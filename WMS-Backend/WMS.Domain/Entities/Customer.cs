using WMS.Domain.Interfaces;

namespace WMS.Domain.Entities;

public class Customer : IMustBelongToTenant
{
    public int Id { get; private set; }
    public int TenantId { get; set; } 
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? ContactEmail { get; private set; }
    public string PhoneNumber { get; private set; } = string.Empty;
    public string? Address { get; private set; }
    public decimal? CreditLimit { get; private set; }
    public bool IsActive { get; private set; }

    private Customer() { }

    public static Customer Create(
        string code,
        string name,
        string? contactEmail,
        string phoneNumber,
        string? address,
        decimal? creditLimit)
    {
        return new Customer
        {
            Code = code,
            Name = name,
            ContactEmail = contactEmail,
            PhoneNumber = phoneNumber,
            Address = address,
            CreditLimit = creditLimit,
            IsActive = true
        };
    }

    public void Update(
        string name,
        string? contactEmail,
        string phoneNumber,
        string? address,
        decimal? creditLimit)
    {
        Name = name;
        ContactEmail = contactEmail;
        PhoneNumber = phoneNumber;
        Address = address;
        CreditLimit = creditLimit;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}