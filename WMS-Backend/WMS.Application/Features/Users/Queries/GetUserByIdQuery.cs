namespace WMS.Application.Features.Users.Queries;

public record GetUserByIdQuery(int Id) : IRequest<UserDto>;

public sealed class GetUserByIdQueryValidator : AbstractValidator<GetUserByIdQuery>
{
    public GetUserByIdQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

internal sealed class GetUserByIdQueryHandler(IWmsDbContext context, ITenantContext tenantContext)
    : IRequestHandler<GetUserByIdQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedAccessException("A tenant workspace is required to view users.");

        return await context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.Id == request.Id && u.TenantId == tenantId)
            .Select(u => new UserDto(
                u.Id,
                u.FullName,
                u.Email,
                u.IsActive,
                u.CreatedAtUtc,
                u.LastLoginAtUtc,
                u.UserRoles.Select(ur => ur.Role.Name).ToList()))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"User with Id {request.Id} not found.");
    }
}
