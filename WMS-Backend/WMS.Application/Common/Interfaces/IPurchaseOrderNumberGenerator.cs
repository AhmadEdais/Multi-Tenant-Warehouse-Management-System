namespace WMS.Application.Common.Interfaces;

public interface IPurchaseOrderNumberGenerator
{
    Task<string> ReserveNextAsync(CancellationToken cancellationToken);
}
