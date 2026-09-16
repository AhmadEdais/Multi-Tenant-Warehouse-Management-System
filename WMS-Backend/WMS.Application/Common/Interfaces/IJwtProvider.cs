namespace WMS.Application.Common.Interfaces;

public interface IJwtProvider
{
    string GenerateToken(User user, IEnumerable<string> roles);
}