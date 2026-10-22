using Microsoft.Extensions.Options;

namespace WMS.API.Services;

// This limits expensive seeding in this API process. SQL capacity admission remains global.
public sealed class DemoProvisioningLimiter(IOptions<DemoOptions> options) : IDisposable
{
    private readonly SemaphoreSlim permits = new SemaphoreSlim(options.Value.MaxConcurrentProvisioning);

    public bool TryAcquire() => permits.Wait(0);

    public void Release() => permits.Release();

    public void Dispose() => permits.Dispose();
}
