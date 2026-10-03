namespace WMS.Application.Features.Products.Queries;

public record ProductCategoryDetailsDto(int CategoryId, string CategoryName);

public record ProductDetailsDto(
    int Id,
    string SKU,
    string Name,
    string? Description,
    string UnitOfMeasure,
    decimal UnitCost,
    decimal UnitPrice,
    int ReorderPoint,
    bool IsActive,
    List<ProductCategoryDetailsDto> Categories);

public record GetProductByIdQuery(int Id) : IRequest<ProductDetailsDto>;

public sealed class GetProductByIdQueryValidator : AbstractValidator<GetProductByIdQuery>
{
    public GetProductByIdQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

internal sealed class GetProductByIdQueryHandler(
    IWmsDbContext context,
    ITenantContext tenantContext,
    ICurrentUserService currentUser) : IRequestHandler<GetProductByIdQuery, ProductDetailsDto>
{
    public async Task<ProductDetailsDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to view products.");

        var canSeeInactive = currentUser.IsInRole(Roles.TenantAdmin)
            || currentUser.IsInRole(Roles.WarehouseManager);
        var query = context.Products.AsNoTracking().Where(p => p.Id == request.Id);
        if (!canSeeInactive)
            query = query.Where(p => p.IsActive);

        var product = await query
            .Select(p => new ProductDetailsDto(
                p.Id,
                p.SKU,
                p.Name,
                p.Description,
                p.UnitOfMeasure,
                p.UnitCost,
                p.UnitPrice,
                p.ReorderPoint,
                p.IsActive,
                p.ProductCategories
                    .Where(pc => canSeeInactive || pc.Category.IsActive)
                    .OrderBy(pc => pc.Category.Name)
                    .Select(pc => new ProductCategoryDetailsDto(pc.CategoryId, pc.Category.Name))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return product ?? throw new NotFoundException("Product was not found.");
    }
}
