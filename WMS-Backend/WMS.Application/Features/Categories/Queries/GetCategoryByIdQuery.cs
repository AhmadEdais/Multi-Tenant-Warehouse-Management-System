namespace WMS.Application.Features.Categories.Queries;

public record CategoryDetailsDto(
    int Id,
    string Name,
    int? ParentCategoryId,
    string? ParentCategoryName,
    bool IsActive);

public record GetCategoryByIdQuery(int Id) : IRequest<CategoryDetailsDto>;

public sealed class GetCategoryByIdQueryValidator : AbstractValidator<GetCategoryByIdQuery>
{
    public GetCategoryByIdQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

internal sealed class GetCategoryByIdQueryHandler(IWmsDbContext context, ITenantContext tenantContext, ICurrentUserService currentUser)
    : IRequestHandler<GetCategoryByIdQuery, CategoryDetailsDto>
{
    public async Task<CategoryDetailsDto> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to view categories.");
        var query = context.Categories.AsNoTracking().Where(c => c.Id == request.Id);
        if (!currentUser.IsInRole(Roles.TenantAdmin) && !currentUser.IsInRole(Roles.WarehouseManager))
            query = query.Where(c => c.IsActive);

        var category = await query
            .Select(c => new CategoryDetailsDto(
                c.Id,
                c.Name,
                c.ParentCategoryId,
                c.ParentCategory == null ? null : c.ParentCategory.Name,
                c.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return category ?? throw new NotFoundException("Category was not found.");
    }
}
