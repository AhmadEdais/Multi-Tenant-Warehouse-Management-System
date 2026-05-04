namespace WMS.Application.Features.Authorization.Commands
{
    public record RegisterUserCommand(string Email, string Password, string FullName) : IRequest<int>;
    public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
    {
        public RegisterUserCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full Name is required.")
                .MaximumLength(200).WithMessage("Full Name cannot exceed 200 characters.");
        }
    }
    internal sealed class RegisterUserCommandHandler(IWmsDbContext context, IPasswordHasher passwordHasher, ITenantContext tenantContext) : IRequestHandler<RegisterUserCommand, int>
    {

        public async Task<int> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            int? currentTenantId = tenantContext.TenantId;

            bool userExists = await context.Users
                .AnyAsync(u => u.Email == request.Email && u.TenantId == currentTenantId, cancellationToken);

            if (userExists)
            {
                throw new ConflictException($"A user with the email '{request.Email}' already exists in this workspace.");
            }

            string passwordHash = passwordHasher.HashPassword(request.Password);
            var user = User.Create(currentTenantId, request.Email, passwordHash, request.FullName);
            context.Users.Add(user);
            await context.SaveChangesAsync(cancellationToken);
            return user.Id;
        }
    }
}