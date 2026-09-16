namespace WMS.Application.Common.Models
{
    public record PagedResult<T>(
        List<T> Data,
        int TotalCount,
        int PageNumber,
        int PageSize);
}