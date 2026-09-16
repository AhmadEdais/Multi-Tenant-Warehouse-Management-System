namespace WMS.Application.Features.Users.Queries;

public record CurrentUserDto(int Id, string Email, string FullName, List<string> Roles);

public record GetCurrentUserQuery() : IRequest<CurrentUserDto>;

internal sealed class GetCurrentUserQueryHandler(IWmsDbContext context, ICurrentUserService currentUserService) : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var user = await context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken) ?? throw new NotFoundException("User not found.");

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        return new CurrentUserDto(user.Id, user.Email, user.FullName, roles);
    }
}