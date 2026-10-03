namespace WMS.Application.Features.Suppliers.Commands;
public record CreateSupplierCommand(
    string Code,
    string Name,
    string? ContactEmail,
    string PhoneNumber, 
    string? Address) : IRequest<int>;
public class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator()
    {
        RuleFor(x => x.Code).Must(value => !string.IsNullOrWhiteSpace(value)).MaximumLength(50);
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
internal sealed class CreateSupplierCommandHandler(IWmsDbContext context,ITenantContext tenantContext) : IRequestHandler<CreateSupplierCommand, int>
{

    public async Task<int> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        if (!tenantContext.TenantId.HasValue)
            throw new UnauthorizedAccessException("A tenant workspace is required to create suppliers.");
        var code = request.Code.Trim();
        var codeExists = await context.Suppliers
            .AnyAsync(s => s.Code == code, cancellationToken);
        if (codeExists)
        {
            throw new ConflictException($"A supplier with the code '{code}' already exists.");
        }
        var supplier = Supplier.Create(
            code,
            request.Name.Trim(),
            string.IsNullOrWhiteSpace(request.ContactEmail) ? null : request.ContactEmail.Trim(),
            request.PhoneNumber.Trim(),
            string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim());
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync(cancellationToken);
        return supplier.Id;
    }
}
