namespace WMS.Application.Features.RoleCatalog.Queries;

public record AssignableRoleDto(int Id, string Name, string Description);

public record GetAssignableRolesQuery() : IRequest<List<AssignableRoleDto>>;

internal sealed class GetAssignableRolesQueryHandler(IWmsDbContext context, ITenantContext tenantContext)
    : IRequestHandler<GetAssignableRolesQuery, List<AssignableRoleDto>>
{
    public async Task<List<AssignableRoleDto>> Handle(GetAssignableRolesQuery request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
        {
            throw new UnauthorizedAccessException("A tenant workspace is required to view assignable roles.");
        }

        var assignableNames = Roles.TenantAssignable.ToArray();
        var roles = await context.Roles
            .AsNoTracking()
            .Where(r => assignableNames.Contains(r.Name))
            .Select(r => new AssignableRoleDto(r.Id, r.Name, r.Description))
            .ToListAsync(cancellationToken);

        return roles
            .OrderBy(r => Array.IndexOf(assignableNames, r.Name))
            .ToList();
    }
}
