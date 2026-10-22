namespace WMS.Application.Features.Tenants.Commands;

public record ProvisionTenantCommand(string TenantCode, string TenantName, string AdminFullName,
    string AdminEmail,string InitialPassword) : IRequest<int>;

public sealed class ProvisionTenantCommandValidator : AbstractValidator<ProvisionTenantCommand>
{
    public ProvisionTenantCommandValidator()
    {
        RuleFor(x => x.TenantCode)
            .NotEmpty().WithMessage("Tenant Code is required.")
            .MaximumLength(20).WithMessage("Tenant Code cannot exceed 20 characters.");

        RuleFor(x => x.TenantName)
            .NotEmpty().WithMessage("Tenant Name is required.")
            .MaximumLength(200).WithMessage("Tenant Name cannot exceed 200 characters.");

        RuleFor(x => x.AdminFullName)
            .NotEmpty().WithMessage("Tenant Administrator full name is required.")
            .MaximumLength(200).WithMessage("Tenant Administrator full name cannot exceed 200 characters.");

        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("Tenant Administrator email is required.")
            .EmailAddress().WithMessage("Tenant Administrator email format is invalid.")
            .MaximumLength(256).WithMessage("Tenant Administrator email cannot exceed 256 characters.");

        RuleFor(x => x.InitialPassword)
            .NotEmpty().WithMessage("An initial password is required.")
            .MinimumLength(5).WithMessage("The initial password must be at least 5 characters long.");

    }
}

internal sealed class ProvisionTenantCommandHandler(
    IWmsDbContext context, ICurrentUserService currentUser, ITenantProvisioningService provisioning)
    : IRequestHandler<ProvisionTenantCommand, int>
{
    public async Task<int> Handle(ProvisionTenantCommand request, CancellationToken cancellationToken)
    {
        var systemAdminId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("An authenticated SystemAdmin is required to provision a tenant.");
        if (!currentUser.IsInRole(Roles.SystemAdmin))
            throw new ForbiddenException("SystemAdmin access is required.");
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var result = await provisioning.CreateNormalAsync(new TenantProvisioningDetails(
            request.TenantCode, request.TenantName, request.AdminFullName, request.AdminEmail,
            request.InitialPassword), systemAdminId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result.Tenant.Id;
    }
}
