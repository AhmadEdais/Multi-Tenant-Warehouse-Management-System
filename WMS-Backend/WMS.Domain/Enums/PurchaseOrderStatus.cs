namespace WMS.Domain.Enums;

public enum PurchaseOrderStatus : byte
{
    Pending = 1,
    Receiving = 2,
    Received = 3,
    Canceled = 4
}