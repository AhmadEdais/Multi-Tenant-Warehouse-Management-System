namespace WMS.Application.Common.Interfaces;

public interface IAuthSessionIssuer
{
    Task<AuthenticationResult> IssueAsync(User user, IReadOnlyList<string> roles, bool rememberMe,
        DateTime? tenantDeadlineUtc, CancellationToken cancellationToken);
}
