namespace WMS.Application.Features.Suppliers.Commands;

public record UpdateSupplierCommand(
    [property: JsonIgnore] int Id,
    string Name,
    string? ContactEmail,
    string PhoneNumber, 
    string? Address) : IRequest;
public class UpdateSupplierCommandValidator : AbstractValidator<UpdateSupplierCommand>
{
    public UpdateSupplierCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Supplier Id is Invalid");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is Required").MaximumLength(200);
        RuleFor(x => x.ContactEmail).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrEmpty(x.ContactEmail));
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Address).MaximumLength(500);
    }
}
internal class UpdateSupplierCommandHandler(IWmsDbContext context) : IRequestHandler<UpdateSupplierCommand>
{
    public async Task Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = await context.Suppliers
            .FindAsync([request.Id], cancellationToken)
             ?? throw new NotFoundException($"Supplier with ID {request.Id} not found.");

        supplier.Update(
            request.Name,
            request.ContactEmail,
            request.PhoneNumber,
            request.Address);
        await context.SaveChangesAsync(cancellationToken);
    }
}
