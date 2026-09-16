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
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactEmail).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrEmpty(x.ContactEmail));
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Address).MaximumLength(500);
    }
}
internal sealed class CreateSupplierCommandHandler(IWmsDbContext context,ITenantContext tenantContext) : IRequestHandler<CreateSupplierCommand, int>
{

    public async Task<int> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("Must be in a tenant context.");
        var codeExists = await context.Suppliers
            .IgnoreQueryFilters()
            .AnyAsync(s => s.TenantId == tenantId && s.Code == request.Code, cancellationToken);
        if (codeExists)
        {
            throw new ConflictException($"A supplier with the code '{request.Code}' already exists.");
        }
        var supplier = Supplier.Create(
            request.Code,
            request.Name,
            request.ContactEmail,
            request.PhoneNumber,
            request.Address);
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync(cancellationToken);
        return supplier.Id;
    }
}