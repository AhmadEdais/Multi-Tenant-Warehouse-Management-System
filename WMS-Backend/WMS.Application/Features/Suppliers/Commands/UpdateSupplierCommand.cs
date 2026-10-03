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
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).Must(value => !string.IsNullOrWhiteSpace(value)).MaximumLength(200);
        RuleFor(x => x.ContactEmail)
            .MaximumLength(256)
            .Must(value => string.IsNullOrWhiteSpace(value) ||
                new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(value.Trim()))
            .WithMessage("Contact email must be a valid email address.");
        RuleFor(x => x.PhoneNumber).Must(value => !string.IsNullOrWhiteSpace(value)).MaximumLength(50);
        RuleFor(x => x.Address).MaximumLength(500);
    }
}
internal class UpdateSupplierCommandHandler(IWmsDbContext context, ITenantContext tenantContext) : IRequestHandler<UpdateSupplierCommand>
{
    public async Task Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to update suppliers.");

        var supplier = await context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
             ?? throw new NotFoundException($"Supplier with ID {request.Id} not found.");
        if (!supplier.IsActive)
            throw new ConflictException("Inactive suppliers cannot be edited.");

        supplier.Update(
            request.Name.Trim(),
            string.IsNullOrWhiteSpace(request.ContactEmail) ? null : request.ContactEmail.Trim(),
            request.PhoneNumber.Trim(),
            string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim());
        await context.SaveChangesAsync(cancellationToken);
    }
}
