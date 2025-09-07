using System.Linq.Dynamic.Core;
using OrderService.Application.Models;
using OrderService.Domain.Enums;
using Shared.Kernel.Application.OperationResults;
using Shared.Kernel.Application.OperationResults.Paging;

namespace OrderService.Application.Services;

public interface IOrderService
{
    Task<OperationResult<OrderDto>> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
    
    Task<OperationResult<OrderDto>> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<OperationResult<IPaginate<OrderDto>>> GetOrdersAsync(OrderQueryParameters queryParameters, CancellationToken cancellationToken = default);
    Task<OperationResult<IPaginate<OrderDto>>> GetOrdersByCustomerIdAsync(string customerId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<OperationResult<IPaginate<OrderDto>>> GetOrdersByStatusAsync(OrderStatus status, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    
    Task<OperationResult<OrderDto>> UpdateOrderAsync(Guid orderId, UpdateOrderRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult<OrderDto>> UpdateOrderStatusAsync(Guid orderId, OrderStatus status, CancellationToken cancellationToken = default);
    
    Task<OperationResult> CancelOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    
    // Stock management operations
    Task<OperationResult> ConfirmStockReservationAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<OperationResult> FailStockReservationAsync(Guid orderId, string reason = "Stock not available", CancellationToken cancellationToken = default);
    Task<OperationResult> ConfirmOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
}