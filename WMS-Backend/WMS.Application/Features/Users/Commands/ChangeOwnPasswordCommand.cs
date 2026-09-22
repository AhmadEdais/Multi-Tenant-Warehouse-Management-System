namespace WMS.Application.Features.Users.Commands;

public record ChangeOwnPasswordCommand(string CurrentPassword, string NewPassword) : IRequest;

public sealed class ChangeOwnPasswordCommandValidator : AbstractValidator<ChangeOwnPasswordCommand>
{
    public ChangeOwnPasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(5).WithMessage("New password must be at least 5 characters long.")
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must differ from the current password.");
    }
}

internal sealed class ChangeOwnPasswordCommandHandler(
    IWmsDbContext context,
    ICurrentUserService currentUser,
    IPasswordHasher passwordHasher) : IRequestHandler<ChangeOwnPasswordCommand>
{
    public async Task Handle(ChangeOwnPasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("An authenticated user is required to change the password.");

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException($"User with Id {userId} not found.");

        if (!passwordHasher.VerifyPassword(user.PasswordHash, request.CurrentPassword))
        {
            throw new UnauthorizedAccessException("Current password is incorrect.");
        }

        user.ChangePassword(passwordHasher.HashPassword(request.NewPassword));
        await context.SaveChangesAsync(cancellationToken);
    }
}
