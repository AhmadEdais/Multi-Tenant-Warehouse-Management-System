using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using WMS.Application.Common.Exceptions;
using WMS.Domain.Constants;

namespace WMS.Infrastructure.Services;

internal sealed class TenantProvisioningService(IWmsDbContext context, IPasswordHasher passwordHasher)
    : ITenantProvisioningService
{
    public Task<ProvisionedTenant> CreateNormalAsync(TenantProvisioningDetails details, int systemAdminId,
        CancellationToken cancellationToken) => CreateAsync(details, systemAdminId, false, cancellationToken);

    public Task<ProvisionedTenant> CreateDemoAsync(CancellationToken cancellationToken)
    {
        var identity = Guid.NewGuid().ToString("N");
        return CreateAsync(new TenantProvisioningDetails("DEMO" + identity[..16],
            "Northstar Supply Distribution", "Demo Administrator", $"demo-{identity}@wms.invalid",
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))), null, true, cancellationToken);
    }

    private async Task<ProvisionedTenant> CreateAsync(TenantProvisioningDetails details,
        int? creatorId, bool isDemo, CancellationToken cancellationToken)
    {
        if (context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Tenant provisioning requires a caller-owned transaction.");
        var code = details.Code.Trim();
        var email = User.NormalizeEmail(details.AdminEmail);
        if (await context.Tenants.AnyAsync(t => t.Code == code, cancellationToken))
            throw new ConflictException($"Tenant Code '{code}' already exists.");
        // Email is globally unique; this checks identity availability, not tenant business data.
        if (await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email, cancellationToken))
            throw new ConflictException($"A user with the email '{email}' already exists.");
        var role = await context.Roles.SingleOrDefaultAsync(r => r.Name == Roles.TenantAdmin, cancellationToken)
            ?? throw new InvalidOperationException("The TenantAdmin role is not configured.");
        var hash = passwordHasher.HashPassword(details.InitialPassword);
        var tenant = isDemo
            ? Tenant.CreateDemo(code, details.Name.Trim(), TimeSpan.FromHours(1))
            : Tenant.Create(code, details.Name.Trim(), creatorId);
        try
        {
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync(cancellationToken);
            var user = User.Create(tenant.Id, email, hash, details.AdminFullName.Trim());
            user.AssignRole(role.Id, creatorId);
            context.Users.Add(user);
            await context.SaveChangesAsync(cancellationToken);
            return new ProvisionedTenant(tenant, user);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException sql
            && sql.Number is 2601 or 2627)
        {
            throw new ConflictException("The tenant code or administrator email is already in use.");
        }
    }
}
