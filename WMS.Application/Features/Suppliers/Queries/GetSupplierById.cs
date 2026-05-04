namespace WMS.Application.Features.Suppliers.Queries;

public record SupplierDto(
    int Id,
    string Code,
    string Name,
    string? ContactEmail,
    string PhoneNumber,
    string? Address);
public record GetSupplierByIdQuery(int Id) : IRequest<SupplierDto>;
public class GetSupplierByIdQueryValidator : AbstractValidator<GetSupplierByIdQuery>
{
    public GetSupplierByIdQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
internal class GetSupplierByIdQueryHandler(IWmsDbContext context) : IRequestHandler<GetSupplierByIdQuery, SupplierDto>
{
    public async Task<SupplierDto> Handle(GetSupplierByIdQuery request, CancellationToken cancellationToken)
    {
        var supplier = await context.Suppliers
            .AsNoTracking()
            .Where(s => s.Id == request.Id)
            .Select(s => new SupplierDto(
                s.Id,
                s.Code,
                s.Name,
                s.ContactEmail,
                s.PhoneNumber,
                s.Address))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Supplier with ID {request.Id} not found.");

        return supplier;
    }
}