namespace WMS.Application.Features.Demo.Interfaces;

public interface IDemoDataSeeder
{
    Task SeedAsync(int tenantId, int adminUserId, CancellationToken cancellationToken);
}
