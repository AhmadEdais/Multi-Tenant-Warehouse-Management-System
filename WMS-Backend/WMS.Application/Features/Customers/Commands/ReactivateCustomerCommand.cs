namespace WMS.Application.Features.Customers.Commands;

public record ReactivateCustomerCommand(int Id) : IRequest;

public sealed class ReactivateCustomerCommandValidator : AbstractValidator<ReactivateCustomerCommand>
{
    public ReactivateCustomerCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

internal sealed class ReactivateCustomerCommandHandler(IWmsDbContext context, ITenantContext tenantContext)
    : IRequestHandler<ReactivateCustomerCommand>
{
    public async Task Handle(ReactivateCustomerCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to reactivate customers.");

        var customer = await context.Customers
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Customer with ID {request.Id} not found.");
        if (customer.IsActive)
            throw new ConflictException("Customer is already active.");

        customer.Reactivate();
        await context.SaveChangesAsync(cancellationToken);
    }
}
