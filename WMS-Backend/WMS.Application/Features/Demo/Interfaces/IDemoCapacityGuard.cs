namespace WMS.Application.Features.Demo.Interfaces;

public interface IDemoCapacityGuard
{
    Task CheckBeforeProvisioningAsync(int maximum, CancellationToken cancellationToken);
    Task CheckBeforeCommitAsync(int maximum, CancellationToken cancellationToken);
}
