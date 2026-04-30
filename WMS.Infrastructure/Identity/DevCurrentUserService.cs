using WMS.Application.Common.Interfaces;

namespace WMS.Infrastructure.Identity;

public class DevCurrentUserService : ICurrentUserService
{
    // DEV-ONLY — replaced in Slice 2 by HttpCurrentUserService
    public int? UserId => 1; // Hardcoded to a fake System Admin ID
    public bool IsAuthenticated => true;
}