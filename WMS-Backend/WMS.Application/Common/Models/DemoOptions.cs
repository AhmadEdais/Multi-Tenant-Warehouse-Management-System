namespace WMS.Application.Common.Models;

public sealed class DemoOptions
{
    public bool Enabled { get; set; }
    // Until cleanup exists, retained (including expired) demos count toward this bound.
    public int MaxActiveTenants { get; set; } = 30;
}
