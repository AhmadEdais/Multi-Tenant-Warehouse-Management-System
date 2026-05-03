namespace WMS.Application.Features.Locations.Commands;

public record UpdateLocationCommand(
        int Id,
        string Name,
        string? Barcode,
        decimal? MaxWeightCapacityKg) : IRequest;

public class UpdateLocationCommandValidator : AbstractValidator<UpdateLocationCommand>
{
    public UpdateLocationCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Barcode).MaximumLength(100);
        RuleFor(x => x.MaxWeightCapacityKg).GreaterThanOrEqualTo(0).When(x => x.MaxWeightCapacityKg.HasValue);
    }
}
internal class UpdateLocationCommandHandler(IWmsDbContext context) : IRequestHandler<UpdateLocationCommand>
{
    private readonly IWmsDbContext _context = context;

    public async Task Handle(UpdateLocationCommand request, CancellationToken cancellationToken)
    {
        var location = await _context.Locations
            .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);

        if (location == null)
            throw new NotFoundException($"Location with ID {request.Id} not found.");

        if (!string.IsNullOrWhiteSpace(request.Barcode))
        {
            var barcodeExists = await _context.Locations
                .AnyAsync(l => l.WarehouseId == location.WarehouseId
                            && l.Barcode == request.Barcode
                            && l.Id != request.Id, cancellationToken);

            if (barcodeExists)
            {
                throw new ConflictException($"A location with barcode '{request.Barcode}' already exists in this warehouse.");
            }
        }

        location.Update(request.Name, request.Barcode, request.MaxWeightCapacityKg);

        await _context.SaveChangesAsync(cancellationToken);
    }
}