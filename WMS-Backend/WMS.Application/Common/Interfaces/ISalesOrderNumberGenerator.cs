namespace WMS.Application.Common.Interfaces;

public interface ISalesOrderNumberGenerator
{
    Task<string> ReserveNextAsync(CancellationToken cancellationToken);
}
