namespace WMS.Application.Common.Models;

public sealed class DemoOptions
{
    public bool Enabled { get; set; }
    // Counts all retained demo rows, including expired rows awaiting cleanup.
    public int MaxActiveTenants { get; set; } = 100;
    public int MaxConcurrentProvisioning { get; set; } = 3;
    public bool CleanupEnabled { get; set; } = true;
    public int CleanupBatchSize { get; set; } = 10;
}
