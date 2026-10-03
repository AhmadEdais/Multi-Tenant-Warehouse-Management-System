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
        RuleFor(x => x.Code).Must(value => !string.IsNullOrWhiteSpace(value)).MaximumLength(50);
        RuleFor(x => x.Name).Must(value => !string.IsNullOrWhiteSpace(value)).MaximumLength(200);
        RuleFor(x => x.ContactEmail)
            .MaximumLength(256)
            .Must(value => string.IsNullOrWhiteSpace(value) ||
                new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(value.Trim()))
            .WithMessage("Contact email must be a valid email address.");
        RuleFor(x => x.PhoneNumber).ValidPhoneNumber();
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, true)
            .When(x => x.CreditLimit.HasValue);

    }
}
internal sealed class CreateCustomerCommandHandler(IWmsDbContext context, ITenantContext tenantContext) : IRequestHandler<CreateCustomerCommand, int>
{
    public async Task<int> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to create customers.");
        var code = request.Code.Trim();
        var codeExists = await context.Customers
            .AnyAsync(c => c.Code == code, cancellationToken);
        if (codeExists)
        {
            throw new ConflictException($"A customer with the code '{code}' already exists.");
        }
        var customer = Customer.Create(
            code,
            request.Name.Trim(),
            string.IsNullOrWhiteSpace(request.ContactEmail) ? null : request.ContactEmail.Trim(),
            request.PhoneNumber.Trim(),
            string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            request.CreditLimit);
        context.Customers.Add(customer);
        await context.SaveChangesAsync(cancellationToken);
        return customer.Id;
    }
}
