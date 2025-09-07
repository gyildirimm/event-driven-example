namespace StockService.Application.DTOs;

public class ReserveStockRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public Guid OrderId { get; set; }
}
