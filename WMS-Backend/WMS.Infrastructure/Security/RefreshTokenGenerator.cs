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

    public bool TryComputeHash(string? token, out byte[] hash)
    {
        hash = [];
        // Login creates 48 random bytes, encoded as exactly 64 Base64Url characters.
        if (token is not { Length: 64 } || token.Any(character =>
                !((character >= 'A' && character <= 'Z') ||
                  (character >= 'a' && character <= 'z') ||
                  (character >= '0' && character <= '9') ||
                  character is '-' or '_')))
            return false;

        hash = SHA256.HashData(Encoding.ASCII.GetBytes(token));
        return true;
    }
}
