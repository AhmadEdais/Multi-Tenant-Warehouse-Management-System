namespace WMS.Application.Features.Authorization.Commands;

public record LoginCommand(string Email, string Password, bool RememberMe = false) : IRequest<AuthenticationResult>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(5);
    }
}

internal sealed class LoginCommandHandler(
    IWmsDbContext context,
    IPasswordHasher passwordHasher,
    IAuthSessionIssuer sessionIssuer)
    : IRequestHandler<LoginCommand, AuthenticationResult>
{
    public async Task<AuthenticationResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = User.NormalizeEmail(request.Email);

        var user = await context.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user == null || !user.IsActive || !passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        DateTime? tenantDeadline = null;
        if (user.TenantId.HasValue)
        {
            var tenant = await context.Tenants.AsNoTracking().FirstOrDefaultAsync(
                t => t.Id == user.TenantId.Value, cancellationToken);
            if (tenant is null || !tenant.IsActive ||
                (tenant.IsDemo && (!tenant.DemoExpiresAtUtc.HasValue || tenant.DemoExpiresAtUtc <= DateTime.UtcNow)))
                throw new UnauthorizedAccessException("Invalid email or password.");
            tenantDeadline = tenant.DemoExpiresAtUtc;
        }
        if(user.UserRoles.Count == 0)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }
        var roleNames = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        return await sessionIssuer.IssueAsync(user, roleNames, request.RememberMe,
            tenantDeadline, cancellationToken);
    }
}
