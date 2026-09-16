namespace WMS.Domain.Entities;

public sealed class User
{
    public int Id { get; private set; }
    public int? TenantId { get; private set; } 
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? LastLoginAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private readonly List<UserRole> _userRoles = [];
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private User() { }

    public static User Create(int? tenantId, string email, string passwordHash, string fullName)
    {
        return new User
        {
            TenantId = tenantId,
            Email = email,
            PasswordHash = passwordHash,
            FullName = fullName,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void RecordLogin()
    {
        LastLoginAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
    }

    public void AssignRole(int roleId, int? assignedByUserId)
    {
        if (!_userRoles.Exists(ur => ur.RoleId == roleId))
        {
            _userRoles.Add(UserRole.Create(this.Id, roleId, assignedByUserId));
        }
    }
    public void AssignToTenant(int tenantId)
    {
        // Optional: Add domain rules here (e.g., "Cannot move a user if they have active tasks")
        TenantId = tenantId;
    }
}