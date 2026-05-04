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
internal sealed class GetCustomerByIdQueryHandler(IWmsDbContext context) : IRequestHandler<GetCustomerByIdQuery, CustomerDto>
{
    public async Task<CustomerDto> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await context.Customers
            .AsNoTracking()
            .Where(c => c.Id == request.Id)
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
