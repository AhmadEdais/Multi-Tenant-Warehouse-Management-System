namespace WMS.Application.Features.Customers.Commands;

public record UpdateCustomerCommand(
    [property: JsonIgnore] int Id,
    string Name,
    string? ContactEmail,
    string PhoneNumber, 
    string? Address,
    decimal? CreditLimit) : IRequest;
public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
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
internal sealed class UpdateCustomerCommandHandler(IWmsDbContext context, ITenantContext tenantContext) : IRequestHandler<UpdateCustomerCommand>
{
    public async Task Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to update customers.");
        var customer = await context.Customers
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Customer with ID {request.Id} not found.");
        if (!customer.IsActive)
            throw new ConflictException("Inactive customers cannot be edited.");

        customer.Update(
            request.Name.Trim(),
            string.IsNullOrWhiteSpace(request.ContactEmail) ? null : request.ContactEmail.Trim(),
            request.PhoneNumber.Trim(),
            string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            request.CreditLimit);
        await context.SaveChangesAsync(cancellationToken);
    }
}
