namespace WMS.Application.Features.Customers.Queries;

public record CustomerDto(
    int Id,
    string Code,
    string Name,
    string? ContactEmail,
    string PhoneNumber,
    string? Address,
    bool IsActive,
    decimal? CreditLimit);
public record GetCustomerByIdQuery(int Id) : IRequest<CustomerDto>;
public class GetCustomerByIdQueryValidator : AbstractValidator<GetCustomerByIdQuery>
{
    public GetCustomerByIdQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
internal sealed class GetCustomerByIdQueryHandler(
    IWmsDbContext context,
    ITenantContext tenantContext,
    ICurrentUserService currentUser) : IRequestHandler<GetCustomerByIdQuery, CustomerDto>
{
    public async Task<CustomerDto> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to view customers.");

        var canSeeInactive = currentUser.IsInRole(Roles.TenantAdmin)
            || currentUser.IsInRole(Roles.WarehouseManager);
        var query = context.Customers
            .AsNoTracking()
            .Where(c => c.Id == request.Id);
        if (!canSeeInactive)
            query = query.Where(c => c.IsActive);

        var customer = await query
            .Select(c => new CustomerDto(
                c.Id,
                c.Code,
                c.Name,
                c.ContactEmail,
                c.PhoneNumber,
                c.Address,
                c.IsActive,
                c.CreditLimit))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Customer with ID {request.Id} not found.");
        
        return customer;
    }
}
