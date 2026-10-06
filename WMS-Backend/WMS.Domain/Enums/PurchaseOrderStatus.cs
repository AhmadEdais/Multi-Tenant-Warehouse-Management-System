namespace WMS.Domain.Enums;

public enum PurchaseOrderStatus : byte
{
    Drafted = 1,
    Pending = 2,
    Receiving = 3,
    Received = 4,
    Canceled = 5
}