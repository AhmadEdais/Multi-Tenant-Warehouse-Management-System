namespace WMS.Application.Common.Interfaces;

public interface IRefreshTokenGenerator
{
    (string Token, byte[] Hash) Generate();
}
