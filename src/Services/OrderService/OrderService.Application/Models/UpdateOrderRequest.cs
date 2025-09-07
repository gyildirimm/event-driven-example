using OrderService.Domain.Enums;

namespace OrderService.Application.Models;

public class UpdateOrderRequest
{
    public OrderStatus Status { get; set; }
}
