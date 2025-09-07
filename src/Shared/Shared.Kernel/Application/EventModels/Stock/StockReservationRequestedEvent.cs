namespace Shared.Kernel.Application.EventModels.Stock;

public class StockReservationRequestedEvent
{
    public Guid OrderId { get; set; }
    public List<StockReservationRequestItem> Items { get; set; } = new();
    public DateTime RequestedAt { get; set; }
}

public class StockReservationRequestItem
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}