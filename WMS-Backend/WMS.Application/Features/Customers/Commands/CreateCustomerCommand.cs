namespace WMS.Application.Features.Customers.Commands;

public record CreateCustomerCommand(
    string Code,
    string Name,
    string? ContactEmail,
    string PhoneNumber, 
    string? Address,
    decimal? CreditLimit) : IRequest<int>;
public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactEmail).MaximumLength(256).EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.ContactEmail));
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0)
            .When(x => x.CreditLimit.HasValue);

    }
}
internal sealed class CreateCustomerCommandHandler(IWmsDbContext context, ITenantContext tenantContext) : IRequestHandler<CreateCustomerCommand, int>
{
    public async Task<int> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("Must be in a tenant context.");
        var codeExists = await context.Customers
            .IgnoreQueryFilters()
            .AnyAsync(c => c.TenantId == tenantId && c.Code == request.Code, cancellationToken);
        if (codeExists)
        {
            throw new ConflictException($"A customer with the code '{request.Code}' already exists.");
        }
        var customer = Customer.Create(
            request.Code,
            request.Name,
            request.ContactEmail,
            request.PhoneNumber,
            request.Address,
            request.CreditLimit);
        context.Customers.Add(customer);
        await context.SaveChangesAsync(cancellationToken);
        return customer.Id;
    }
}