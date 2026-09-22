namespace WMS.Application.Features.Users.Queries;

public record UserDto(
    int Id,
    string FullName,
    string Email,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc,
    List<string> Roles);

public record ListUsersQuery(string? SearchTerm = null, int PageNumber = 1, int PageSize = 10)
    : IRequest<PagedResult<UserDto>>;

public sealed class ListUsersQueryValidator : AbstractValidator<ListUsersQuery>
{
    public ListUsersQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}

internal sealed class ListUsersQueryHandler(IWmsDbContext context, ITenantContext tenantContext)
    : IRequestHandler<ListUsersQuery, PagedResult<UserDto>>
{
    public async Task<PagedResult<UserDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedAccessException("A tenant workspace is required to list users.");

        var query = context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();
            query = query.Where(u => u.FullName.Contains(searchTerm) || u.Email.Contains(searchTerm));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderBy(u => u.FullName)
            .ThenBy(u => u.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserDto(
                u.Id,
                u.FullName,
                u.Email,
                u.IsActive,
                u.CreatedAtUtc,
                u.LastLoginAtUtc,
                u.UserRoles.Select(ur => ur.Role.Name).ToList()))
            .ToListAsync(cancellationToken);

        return new PagedResult<UserDto>(users, totalCount, request.PageNumber, request.PageSize);
    }
}
