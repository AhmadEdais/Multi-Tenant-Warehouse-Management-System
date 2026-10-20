namespace WMS.Application.Common.Models;

public sealed class AuthSessionPolicy
{
    public int NonPersistentIdleMinutes { get; set; }
    public int NonPersistentAbsoluteMinutes { get; set; }
    public int PersistentIdleHours { get; set; }
    public int PersistentAbsoluteDays { get; set; }

    public bool IsValid() => NonPersistentIdleMinutes > 0
        && NonPersistentAbsoluteMinutes >= NonPersistentIdleMinutes
        && PersistentIdleHours > 0
        && PersistentAbsoluteDays > 0
        && TimeSpan.FromHours(PersistentIdleHours) <= TimeSpan.FromDays(PersistentAbsoluteDays);

    public (DateTime Idle, DateTime Absolute) GetDeadlines(DateTime nowUtc, bool rememberMe)
    {
        var absolute = rememberMe
            ? nowUtc.AddDays(PersistentAbsoluteDays)
            : nowUtc.AddMinutes(NonPersistentAbsoluteMinutes);
        var idle = rememberMe
            ? nowUtc.AddHours(PersistentIdleHours)
            : nowUtc.AddMinutes(NonPersistentIdleMinutes);
        return (idle < absolute ? idle : absolute, absolute);
    }

    public DateTime GetRefreshedIdleDeadline(DateTime nowUtc, bool rememberMe,
        DateTime absoluteExpiresAtUtc)
    {
        var idle = rememberMe
            ? nowUtc.AddHours(PersistentIdleHours)
            : nowUtc.AddMinutes(NonPersistentIdleMinutes);
        return idle < absoluteExpiresAtUtc ? idle : absoluteExpiresAtUtc;
    }
}
