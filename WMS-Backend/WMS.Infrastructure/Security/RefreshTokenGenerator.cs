using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace WMS.Infrastructure.Security;

internal sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    public (string Token, byte[] Hash) Generate()
    {
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(48));
        return (token, SHA256.HashData(Encoding.ASCII.GetBytes(token)));
    }
}
