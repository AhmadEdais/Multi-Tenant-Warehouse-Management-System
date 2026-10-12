namespace WMS.Domain.Enums;

public enum SalesOrderStatus : byte
{
    Draft = 1,
    Allocated = 2,
    Shipped = 3,
    Canceled = 4
}
