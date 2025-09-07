namespace OrderService.Domain.Enums;

public enum OrderStatus
{
    Pending = 0,
    AwaitingStockReservation = 1,
    StockReserved = 2,
    StockReservationFailed = 3,
    Confirmed = 4,
    Shipped = 5,
    Delivered = 6,
    Cancelled = 7
}