namespace WMS.Application.Features.Suppliers.Commands;

public record ReactivateSupplierCommand(int Id) : IRequest;

public sealed class ReactivateSupplierCommandValidator : AbstractValidator<ReactivateSupplierCommand>
{
    public ReactivateSupplierCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

internal sealed class ReactivateSupplierCommandHandler(IWmsDbContext context, ITenantContext tenantContext)
    : IRequestHandler<ReactivateSupplierCommand>
{
    public async Task Handle(ReactivateSupplierCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to reactivate suppliers.");

        var supplier = await context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Supplier with ID {request.Id} not found.");
        if (supplier.IsActive)
            throw new ConflictException("Supplier is already active.");

        supplier.Reactivate();
        await context.SaveChangesAsync(cancellationToken);
    }
}
