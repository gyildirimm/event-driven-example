using OrderService.Domain.Enums;

namespace OrderService.Application.Models;

public class OrderDto
{
    public Guid Id { get; set; }
    public string CustomerId { get; set; } = default!;
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderLineDto> OrderLines { get; set; } = new();
}
