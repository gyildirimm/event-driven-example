using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderService.Application.Models;
using OrderService.Application.Services;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Persistence.Contexts;
using Shared.Kernel.Application.EventModels.Stock;
using Shared.Kernel.Application.OperationResults;
using Shared.Kernel.Application.OperationResults.Paging;
using Shared.Kernel.Application.Repositories;

namespace OrderService.Persistence.Services;

public class OrderApplicationService(IUnitOfWork<OrderContext> unitOfWork, ILogger<OrderApplicationService> logger) : IOrderService
{
    private readonly IUnitOfWork<OrderContext> _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly ILogger<OrderApplicationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<OperationResult<OrderDto>> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var order = new Order(request.CustomerId, request.CustomerEmail);

            foreach (var lineRequest in request.OrderLines)
            {
                order.AddOrderLine(lineRequest.ProductId, lineRequest.ProductName, lineRequest.UnitPrice, lineRequest.Quantity);
            }

            order.RequestStockReservation();

            var repository = _unitOfWork.GetAsyncRepository<Order, Guid>();
            await repository.AddAsync(order);

            var outboxRepository = _unitOfWork.GetAsyncRepository<OutboxEvent, Guid>();
            var orderLineRepository = _unitOfWork.GetAsyncRepository<OrderLine, Guid>();

            await orderLineRepository.AddRangeAsync(order.OrderLines, cancellationToken);
            
            var stockReservationEvent = new StockReservationRequestedEvent
            {
                OrderId = order.Id,
                Items = order.OrderLines.Select(ol => new StockReservationRequestItem
                {
                    ProductId = Guid.Parse(ol.ProductId),
                    Quantity = ol.Quantity
                }).ToList(),
                RequestedAt = DateTime.UtcNow
            };

            var eventData = JsonSerializer.Serialize(stockReservationEvent);
            var outboxEvent = new OutboxEvent("StockReservationRequested", eventData, "stock-events");
            
            await outboxRepository.AddAsync(outboxEvent);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(transaction, cancellationToken);
            var orderDto = MapToDto(order);
            return OperationResult<OrderDto>.Success(orderDto, "Order created successfully", 201);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for customer: {CustomerId}", request.CustomerId);
            await _unitOfWork.RollbackTransactionAsync(transaction, cancellationToken);
            return OperationResult<OrderDto>.Fail($"Error creating order: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult<OrderDto>> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _unitOfWork.GetAsyncRepository<Order, Guid>();
            var order = await repository.GetAsync(o => o.Id == orderId, cancellationToken: cancellationToken);
            
            if (order == null)
                return OperationResult<OrderDto>.Fail($"Order with ID {orderId} not found", statusCode: 404);

            var orderDto = MapToDto(order);
            return OperationResult<OrderDto>.Success(orderDto, "Order retrieved successfully");
        }
        catch (Exception ex)
        {
            return OperationResult<OrderDto>.Fail($"Error retrieving order: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult<IPaginate<OrderDto>>> GetOrdersAsync(OrderQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _unitOfWork.GetAsyncRepository<Order, Guid>();
            
            var result = await repository.GetListAsync(
                index: queryParameters.Page - 1,
                size: queryParameters.PageSize,
                cancellationToken: cancellationToken);
            
            var paginatedResult = Paginate.From(result, items => items.Select(MapToDto));
            return OperationResult<IPaginate<OrderDto>>.Success(paginatedResult, "Orders retrieved successfully");
        }
        catch (Exception ex)
        {
            return OperationResult<IPaginate<OrderDto>>.Fail($"Error retrieving orders: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult<IPaginate<OrderDto>>> GetOrdersByCustomerIdAsync(string customerId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _unitOfWork.GetAsyncRepository<Order, Guid>();
            var result = await repository.GetListAsync(
                predicate: o => o.CustomerId == customerId,
                index: page - 1,
                size: pageSize,
                cancellationToken: cancellationToken);
            
            var paginatedResult = Paginate.From(result, items => items.Select(MapToDto));
            return OperationResult<IPaginate<OrderDto>>.Success(paginatedResult, "Customer orders retrieved successfully");
        }
        catch (Exception ex)
        {
            return OperationResult<IPaginate<OrderDto>>.Fail($"Error retrieving customer orders: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult<IPaginate<OrderDto>>> GetOrdersByStatusAsync(OrderStatus status, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _unitOfWork.GetAsyncRepository<Order, Guid>();
            var result = await repository.GetListAsync(
                predicate: o => o.Status == status,
                index: page - 1,
                size: pageSize,
                cancellationToken: cancellationToken);

            var paginatedResult = Paginate.From(result, items => items.Select(MapToDto));
            return OperationResult<IPaginate<OrderDto>>.Success(paginatedResult, "Orders by status retrieved successfully");
        }
        catch (Exception ex)
        {
            return OperationResult<IPaginate<OrderDto>>.Fail($"Error retrieving orders by status: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult<OrderDto>> UpdateOrderAsync(Guid orderId, UpdateOrderRequest request, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var repository = _unitOfWork.GetAsyncRepository<Order, Guid>();
            var order = await repository.GetAsync(o => o.Id == orderId, cancellationToken: cancellationToken);
            
            if (order == null)
                return OperationResult<OrderDto>.Fail($"Order with ID {orderId} not found", statusCode: 404);

            // Basic status update
            if (request.Status == OrderStatus.Confirmed)
                order.ConfirmOrder();
            else if (request.Status == OrderStatus.Cancelled)
                order.CancelOrder();

            await repository.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(transaction, cancellationToken);

            var orderDto = MapToDto(order);
            return OperationResult<OrderDto>.Success(orderDto, "Order updated successfully");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction, cancellationToken);
            return OperationResult<OrderDto>.Fail($"Error updating order: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult<OrderDto>> UpdateOrderStatusAsync(Guid orderId, OrderStatus status, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var repository = _unitOfWork.GetAsyncRepository<Order, Guid>();
            var order = await repository.GetAsync(o => o.Id == orderId, cancellationToken: cancellationToken);
            
            if (order == null)
                return OperationResult<OrderDto>.Fail($"Order with ID {orderId} not found", statusCode: 404);

            switch (status)
            {
                case OrderStatus.Confirmed:
                    order.ConfirmOrder();
                    break;
                case OrderStatus.Cancelled:
                    order.CancelOrder();
                    break;
            }

            await repository.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(transaction, cancellationToken);

            var orderDto = MapToDto(order);
            return OperationResult<OrderDto>.Success(orderDto, "Order status updated successfully");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction, cancellationToken);
            return OperationResult<OrderDto>.Fail($"Error updating order status: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult> CancelOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var repository = _unitOfWork.GetAsyncRepository<Order, Guid>();
            var order = await repository.GetAsync(o => o.Id == orderId, cancellationToken: cancellationToken);
            
            if (order == null)
                return OperationResult.Fail($"Order with ID {orderId} not found", statusCode: 404);

            order.CancelOrder();
            await repository.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(transaction, cancellationToken);

            return OperationResult.Success("Order cancelled successfully");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction, cancellationToken);
            return OperationResult.Fail($"Error cancelling order: {ex.Message}", statusCode: 500);
        }
    }
    
    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            OrderDate = order.CreatedAt,
            Status = order.Status,
            TotalAmount = order.TotalAmount.Amount,
            OrderLines = order.OrderLines.Select(MapToOrderLineDto).ToList()
        };
    }

    private static OrderLineDto MapToOrderLineDto(OrderLine orderLine)
    {
        return new OrderLineDto
        {
            Id = orderLine.Id,
            OrderId = orderLine.Id, // This needs proper OrderId from relationship
            ProductId = orderLine.ProductId,
            ProductName = orderLine.ProductName,
            Quantity = orderLine.Quantity,
            UnitPrice = orderLine.UnitPrice.Amount,
            TotalPrice = orderLine.TotalPrice.Amount
        };
    }

    // Stok yönetimi metodları
    public async Task<OperationResult> ConfirmStockReservationAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var repository = _unitOfWork.GetAsyncRepository<Order, Guid>();
            var order = await repository.GetAsync(o => o.Id == orderId, cancellationToken: cancellationToken);
            
            if (order == null)
                return OperationResult.Fail($"Order with ID {orderId} not found", statusCode: 404);

            order.MarkStockReserved();
            await repository.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(transaction, cancellationToken);

            return OperationResult.Success("Stock reservation confirmed successfully");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction, cancellationToken);
            return OperationResult.Fail($"Error confirming stock reservation: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult> FailStockReservationAsync(Guid orderId, string reason = "Stock not available", CancellationToken cancellationToken = default)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var repository = _unitOfWork.GetAsyncRepository<Order, Guid>();
            var order = await repository.GetAsync(o => o.Id == orderId, cancellationToken: cancellationToken);
            
            if (order == null)
                return OperationResult.Fail($"Order with ID {orderId} not found", statusCode: 404);

            order.MarkStockReservationFailed(reason);
            await repository.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(transaction, cancellationToken);

            return OperationResult.Success("Stock reservation marked as failed");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction, cancellationToken);
            return OperationResult.Fail($"Error marking stock reservation as failed: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult> ConfirmOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _unitOfWork.GetAsyncRepository<Order, Guid>();
            var order = await repository.GetAsync(o => o.Id == orderId, include: q => q.Include(o => o.OrderLines).Include(o => o.OutboxEvents) , cancellationToken: cancellationToken);
            
            if (order == null)
                return OperationResult.Fail($"Order with ID {orderId} not found", statusCode: 404);

            order.ConfirmOrder();
            var outboxRepository = _unitOfWork.GetAsyncRepository<OutboxEvent, Guid>();
        
            var newOutboxEvents = order.OutboxEvents.Where(e => 
                _unitOfWork.GetDbContext().Entry(e).State == EntityState.Detached).ToList();
            
            foreach (var outboxEvent in newOutboxEvents)
            {
                await outboxRepository.AddAsync(outboxEvent);
            }
            
            await repository.UpdateAsync(order);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return OperationResult.Success("Order confirmed successfully");
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Error confirming order: {ex.Message}", statusCode: 500);
        }
    }
}