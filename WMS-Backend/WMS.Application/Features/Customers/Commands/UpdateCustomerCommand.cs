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
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactEmail).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrEmpty(x.ContactEmail));
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0).When(x => x.CreditLimit.HasValue);
    }
}
internal sealed class UpdateCustomerCommandHandler(IWmsDbContext context) : IRequestHandler<UpdateCustomerCommand>
{
    public async Task Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    { 
        var customer = await context.Customers
            .FindAsync([request.Id], cancellationToken)
            ?? throw new NotFoundException($"Customer with ID {request.Id} not found.");

        customer.Update(
            request.Name,
            request.ContactEmail,
            request.PhoneNumber,
            request.Address,
            request.CreditLimit);
        await context.SaveChangesAsync(cancellationToken);
    }
}