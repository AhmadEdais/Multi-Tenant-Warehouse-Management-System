namespace WMS.Application.Features.Suppliers.Commands;

public record DeactivateSupplierCommand(int Id) : IRequest;
public class DeactivateSupplierCommandValidator : AbstractValidator<DeactivateSupplierCommand>
{
    public DeactivateSupplierCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Supplier Id must be greater than 0.");
    }
}
internal class DeactivateSupplierCommandHandler(IWmsDbContext context, ITenantContext tenantContext) : IRequestHandler<DeactivateSupplierCommand>
{
    public async Task Handle(DeactivateSupplierCommand request, CancellationToken cancellationToken)
    {

        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to deactivate suppliers.");

        var supplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
           ?? throw new NotFoundException($"Supplier with Id {request.Id} not found.");
        if (!supplier.IsActive)
            throw new ConflictException("Supplier is already inactive.");

        supplier.Deactivate();
        await context.SaveChangesAsync(cancellationToken);
    }
}
