namespace WMS.Domain.Entities;

public sealed class Role
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    private readonly List<UserRole> _userRoles = [];
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private Role() { }

    public static Role Create(string name, string description)
    {
        return new Role
        {
            Name = name,
            Description = description
        };
    }
}