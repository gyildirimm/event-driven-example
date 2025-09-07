namespace OrderService.Application.Models;

public class CreateOrderRequest
{
    public string CustomerId { get; set; } = default!;
    public string CustomerEmail { get; set; } = default!;
    public List<CreateOrderLineRequest> OrderLines { get; set; } = new();
}
