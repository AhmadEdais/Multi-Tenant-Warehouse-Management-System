namespace WMS.Application.Features.Customers.Commands;

public record DeactivateCustomerCommand(int Id) : IRequest;
public class DeactivateCustomerCommandValidator : AbstractValidator<DeactivateCustomerCommand>
{
    public DeactivateCustomerCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
internal sealed class DeactivateCustomerCommandHandler(IWmsDbContext context, ITenantContext tenantContext) : IRequestHandler<DeactivateCustomerCommand>
{
    public async Task Handle(DeactivateCustomerCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to deactivate customers.");
        var customer = await context.Customers.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Customer with Id {request.Id} not found.");
        if (!customer.IsActive)
            throw new ConflictException("Customer is already inactive.");

        customer.Deactivate();
        await context.SaveChangesAsync(cancellationToken);
    }
}
