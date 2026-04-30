namespace WMS.Application.Features.Tenants.Commands;

public class CreateTenantCommand : IRequest<int>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}


internal class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
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

internal class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, int>
{
    private readonly IWmsDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateTenantCommandHandler(IWmsDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        int? userId = _currentUser.IsAuthenticated ? _currentUser.UserId : null;
        var tenant =  Tenant.Create(request.Code, request.Name, userId);
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(cancellationToken);

        return tenant.Id;
    }
}