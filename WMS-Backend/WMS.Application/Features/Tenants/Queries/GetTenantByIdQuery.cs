namespace WMS.Application.Features.Tenants.Queries;

public record GetTenantByIdQuery(int Id) : IRequest<TenantDto>;

public sealed class GetTenantByIdQueryValidator : AbstractValidator<GetTenantByIdQuery>
{
    public GetTenantByIdQueryValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

internal sealed class GetTenantByIdQueryHandler(IWmsDbContext context)
    : IRequestHandler<GetTenantByIdQuery, TenantDto>
{
    public async Task<TenantDto> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        return await context.Tenants
            .AsNoTracking()
            .Where(t => t.Id == request.Id)
            .Select(t => new TenantDto(t.Id, t.Code, t.Name, t.IsActive, t.CreatedAtUtc, t.CreatedByUserId))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Tenant with Id {request.Id} not found.");
    }
}
