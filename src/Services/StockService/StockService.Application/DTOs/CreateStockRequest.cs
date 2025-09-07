namespace StockService.Application.DTOs;

public class CreateStockRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
