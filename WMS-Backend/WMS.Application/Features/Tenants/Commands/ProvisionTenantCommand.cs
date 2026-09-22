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
    IWmsDbContext context,
    ICurrentUserService currentUser,
    IPasswordHasher passwordHasher) : IRequestHandler<ProvisionTenantCommand, int>
{
    public async Task<int> Handle(ProvisionTenantCommand request, CancellationToken cancellationToken)
    {
        var systemAdminId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("An authenticated SystemAdmin is required to provision a tenant.");

        var tenantCode = request.TenantCode.Trim();
        var tenantName = request.TenantName.Trim();
        var adminEmail = User.NormalizeEmail(request.AdminEmail);
        var adminFullName = request.AdminFullName.Trim();

        var tenantCodeExists = await context.Tenants
            .AnyAsync(t => t.Code == tenantCode, cancellationToken);

        if (tenantCodeExists)
        {
            throw new ConflictException($"Tenant Code '{tenantCode}' already exists.");
        }

        var emailExists = await context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == adminEmail, cancellationToken);

        if (emailExists)
        {
            throw new ConflictException($"A user with the email '{adminEmail}' already exists.");
        }

        var tenantAdminRole = await context.Roles
            .SingleOrDefaultAsync(r => r.Name == Roles.TenantAdmin, cancellationToken)
            ?? throw new InvalidOperationException("The TenantAdmin role is not configured.");

        var passwordHash = passwordHasher.HashPassword(request.InitialPassword);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var tenant = Tenant.Create(tenantCode, tenantName, systemAdminId);
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync(cancellationToken);

            var tenantAdmin = User.Create(
                tenant.Id,
                adminEmail,
                passwordHash,
                adminFullName);

            context.Users.Add(tenantAdmin);
            await context.SaveChangesAsync(cancellationToken);

            tenantAdmin.AssignRole(tenantAdminRole.Id, systemAdminId);
            await context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return tenant.Id;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
