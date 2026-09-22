namespace WMS.Application.Features.Users.Commands;

public record CreateUserCommand(
    string FullName,
    string Email,
    string InitialPassword,
    IReadOnlyCollection<int> RoleIds) : IRequest<int>;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(200).WithMessage("Full name cannot exceed 200 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.InitialPassword)
            .NotEmpty().WithMessage("An initial password is required.")
            .MinimumLength(5).WithMessage("The initial password must be at least 5 characters long.");

        RuleFor(x => x.RoleIds)
            .NotNull().WithMessage("At least one role is required.")
            .NotEmpty().WithMessage("At least one role is required.")
            .Must(roleIds => roleIds is null || roleIds.Count == roleIds.Distinct().Count())
            .WithMessage("Role IDs must be unique.");

        RuleForEach(x => x.RoleIds)
            .GreaterThan(0).WithMessage("Role IDs must be greater than zero.");
    }
}

internal sealed class CreateUserCommandHandler(
    IWmsDbContext context,
    ITenantContext tenantContext,
    ICurrentUserService currentUser,
    IPasswordHasher passwordHasher) : IRequestHandler<CreateUserCommand, int>
{
    private static readonly HashSet<string> AssignableRoleNames =
    [
        Roles.TenantAdmin,
        Roles.WarehouseManager,
        Roles.WarehouseOperator,
        Roles.Analyst
    ];

    public async Task<int> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedAccessException("A tenant workspace is required to create a user.");

        var tenantAdminId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("An authenticated TenantAdmin is required to create a user.");

        var normalizedEmail = User.NormalizeEmail(request.Email);

        var emailExists = await context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            throw new ConflictException($"A user with the email '{normalizedEmail}' already exists.");
        }

        var requestedRoleIds = request.RoleIds.Distinct().ToArray();
        var selectedRoles = await context.Roles
            .Where(r => requestedRoleIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        if (selectedRoles.Count != requestedRoleIds.Length)
        {
            throw new NotFoundException("One or more selected roles do not exist.");
        }

        if (selectedRoles.Any(r => r.Name == Roles.SystemAdmin))
        {
            throw new UnauthorizedAccessException("The SystemAdmin role cannot be assigned through this endpoint.");
        }

        if (selectedRoles.Any(r => !AssignableRoleNames.Contains(r.Name)))
        {
            throw new UnauthorizedAccessException("One or more selected roles are not assignable tenant roles.");
        }

        var passwordHash = passwordHasher.HashPassword(request.InitialPassword);

        var user = User.Create(
            tenantId,
            normalizedEmail,
            passwordHash,
            request.FullName.Trim());

        foreach (var role in selectedRoles)
        {
            user.AssignRole(role.Id, tenantAdminId);
        }

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
