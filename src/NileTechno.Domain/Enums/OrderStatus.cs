namespace NileTechno.Domain.Enums;

public enum OrderStatus
{
    Pending = 0,
    Processing = 1,
    Packed = 2,
    Shipped = 3,
    OutForDelivery = 4,
    Delivered = 5,
    Canceled = 6,
    Refunded = 7
}
