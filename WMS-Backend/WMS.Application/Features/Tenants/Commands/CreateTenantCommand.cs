namespace WMS.Application.Features.Tenants.Commands;

public record CreateTenantCommand(string Code, string Name) : IRequest<int>;

public sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Tenant Code is required.")
            .MaximumLength(20).WithMessage("Tenant Code cannot exceed 20 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tenant Name is required.")
            .MaximumLength(200).WithMessage("Tenant Name cannot exceed 200 characters.");
    }
}

internal sealed class CreateTenantCommandHandler(IWmsDbContext context, ICurrentUserService currentUser) : IRequestHandler<CreateTenantCommand, int>
{
    public async Task<int> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        int? userId = currentUser.IsAuthenticated ? currentUser.UserId : null;
        var code = await context.Tenants
            .Where(t => t.Code == request.Code)
            .FirstOrDefaultAsync(cancellationToken);
        if (code != null)
        {
            throw new ConflictException($"Tenant Code '{request.Code}' already exists.");
        }
        var tenant =  Tenant.Create(request.Code, request.Name, userId);
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync(cancellationToken);

        return tenant.Id;
    }
}