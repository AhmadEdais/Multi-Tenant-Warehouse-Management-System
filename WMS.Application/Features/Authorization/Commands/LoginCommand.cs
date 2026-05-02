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

internal sealed class LoginCommandHandler(
    IWmsDbContext context,
    IPasswordHasher passwordHasher,
    IJwtProvider jwtProvider) : IRequestHandler<LoginCommand, string>
{
    private readonly IWmsDbContext _context = context;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly IJwtProvider _jwtProvider = jwtProvider;

    public async Task<string> Handle(LoginCommand request, CancellationToken cancellationToken)
    {

        var user = await _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null || !user.IsActive || !_passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        user.RecordLogin();

        await _context.SaveChangesAsync(cancellationToken);
        var roleNames = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        return _jwtProvider.GenerateToken(user, roleNames);
    }
}