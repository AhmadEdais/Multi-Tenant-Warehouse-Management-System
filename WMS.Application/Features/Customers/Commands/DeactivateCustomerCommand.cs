namespace WMS.Application.Features.Customers.Commands;

public record DeactivateCustomerCommand(int Id) : IRequest;
public class DeactivateCustomerCommandValidator : AbstractValidator<DeactivateCustomerCommand>
{
    public DeactivateCustomerCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
internal sealed class DeactivateCustomerCommandHandler(IWmsDbContext context) : IRequestHandler<DeactivateCustomerCommand>
{
    public async Task Handle(DeactivateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await context.Customers.FindAsync([ request.Id ], cancellationToken)
            ?? throw new NotFoundException($"Customer with Id {request.Id} not found.");

        customer.Deactivate();
        await context.SaveChangesAsync(cancellationToken);
    }
}