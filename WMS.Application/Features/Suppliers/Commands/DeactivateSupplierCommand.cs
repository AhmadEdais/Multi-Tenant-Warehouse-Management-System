namespace WMS.Application.Features.Suppliers.Commands;

public record DeactivateSupplierCommand(int Id) : IRequest;
public class DeactivateSupplierCommandValidator : AbstractValidator<DeactivateSupplierCommand>
{
    public DeactivateSupplierCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Supplier Id must be greater than 0.");
    }
}
internal class DeactivateSupplierCommandHandler(IWmsDbContext context) : IRequestHandler<DeactivateSupplierCommand>
{
    public async Task Handle(DeactivateSupplierCommand request, CancellationToken cancellationToken)
    {

        var supplier = await context.Suppliers.FindAsync([request.Id], cancellationToken)
           ?? throw new NotFoundException($"Supplier with Id {request.Id} not found.");

        supplier.Deactivate();
        await context.SaveChangesAsync(cancellationToken);
    }
}