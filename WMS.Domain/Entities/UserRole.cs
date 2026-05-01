namespace WMS.Domain.Entities;

public sealed class UserRole
{
    public int UserId { get; private set; }
    public int RoleId { get; private set; }
    public DateTime AssignedAtUtc { get; private set; }
    public int? AssignedByUserId { get; private set; }

    public User User { get; private set; } = null!;
    public Role Role { get; private set; } = null!;

    private UserRole() { }

    public static UserRole Create(int userId, int roleId, int? assignedByUserId)
    {
        return new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            AssignedAtUtc = DateTime.UtcNow,
            AssignedByUserId = assignedByUserId
        };
    }
}