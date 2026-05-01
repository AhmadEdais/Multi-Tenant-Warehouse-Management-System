namespace WMS.Application.Features.Authorization.Commands;

public record LoginCommand(string Email, string Password) : IRequest<string>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

internal sealed class LoginCommandHandler : IRequestHandler<LoginCommand, string>
{
    private readonly IWmsDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtProvider _jwtProvider;
    private readonly ITenantContext _tenantContext;

    public LoginCommandHandler(
        IWmsDbContext context,
        IPasswordHasher passwordHasher,
        IJwtProvider jwtProvider,
        ITenantContext tenantContext)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtProvider = jwtProvider;
        _tenantContext = tenantContext;
    }

    public async Task<string> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.TenantId == tenantId, cancellationToken);

        if (user == null || !_passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        user.RecordLogin();

        await _context.SaveChangesAsync(cancellationToken);
        var roleNames = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        return _jwtProvider.GenerateToken(user, roleNames);
    }
}