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
    IJwtProvider jwtProvider,
    IRefreshTokenGenerator refreshTokenGenerator,
    Microsoft.Extensions.Options.IOptions<AuthSessionPolicy> sessionPolicy)
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

        if (user.TenantId.HasValue && !await context.Tenants
                .AnyAsync(t => t.Id == user.TenantId.Value && t.IsActive, cancellationToken))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }
        if(user.UserRoles.Count == 0)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }
        var roleNames = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var nowUtc = DateTime.UtcNow;
        var (idleDeadline, absoluteDeadline) = sessionPolicy.Value.GetDeadlines(nowUtc, request.RememberMe);
        var session = AuthSession.Create(user.Id, nowUtc, idleDeadline, absoluteDeadline, request.RememberMe);
        var (refreshToken, tokenHash) = refreshTokenGenerator.Generate();
        session.AddInitialRefreshToken(tokenHash, idleDeadline);

        user.RecordLogin();
        context.AuthSessions.Add(session);
        // EF Core saves the session and its token graph in one atomic SaveChanges call.
        await context.SaveChangesAsync(cancellationToken);

        var accessToken = jwtProvider.GenerateToken(user, roleNames, session.Id);
        return new AuthenticationResult(accessToken.AccessToken, accessToken.ExpiresIn,
            refreshToken, request.RememberMe, absoluteDeadline);
    }
}
