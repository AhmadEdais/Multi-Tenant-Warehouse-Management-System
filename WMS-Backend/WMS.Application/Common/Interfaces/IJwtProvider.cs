namespace WMS.Application.Common.Interfaces;

public interface IJwtProvider
{
    AccessTokenResponse GenerateToken(User user, IEnumerable<string> roles, int sessionId);
}
