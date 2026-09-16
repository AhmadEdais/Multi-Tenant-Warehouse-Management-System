namespace WMS.Domain.Enums;

public enum MovementType : byte
{
    Receipt = 1,
    Shipment = 2,
    Adjustment = 3,
    TransferIn = 4,
    TransferOut = 5
}