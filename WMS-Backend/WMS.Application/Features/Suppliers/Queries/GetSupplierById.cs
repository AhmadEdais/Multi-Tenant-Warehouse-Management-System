namespace WMS.Application.Features.Suppliers.Queries;

public record SupplierDto(
    int Id,
    string Code,
    string Name,
    string? ContactEmail,
    string PhoneNumber,
    string? Address,
    bool IsActive);
public record GetSupplierByIdQuery(int Id) : IRequest<SupplierDto>;
public class GetSupplierByIdQueryValidator : AbstractValidator<GetSupplierByIdQuery>
{
    public GetSupplierByIdQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
internal class GetSupplierByIdQueryHandler(
    IWmsDbContext context,
    ITenantContext tenantContext,
    ICurrentUserService currentUser) : IRequestHandler<GetSupplierByIdQuery, SupplierDto>
{
    public async Task<SupplierDto> Handle(GetSupplierByIdQuery request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to view suppliers.");

        var canSeeInactive = currentUser.IsInRole(Roles.TenantAdmin)
            || currentUser.IsInRole(Roles.WarehouseManager);
        var query = context.Suppliers
            .AsNoTracking()
            .Where(s => s.Id == request.Id);
        if (!canSeeInactive)
            query = query.Where(s => s.IsActive);

        var supplier = await query
            .Select(s => new SupplierDto(
                s.Id,
                s.Code,
                s.Name,
                s.ContactEmail,
                s.PhoneNumber,
                s.Address,
                s.IsActive))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Supplier with ID {request.Id} not found.");

        return supplier;
    }
}
