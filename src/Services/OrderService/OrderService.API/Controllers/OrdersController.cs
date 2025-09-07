using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OrderService.Application.Models;
using OrderService.Application.Services;
using OrderService.Domain.Enums;
using Shared.Kernel.Application.OperationResults;

namespace OrderService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tüm siparişleri getirir
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] OrderQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetOrdersAsync(queryParameters, cancellationToken);
        
        if (!result.IsSuccessful)
            return StatusCode(result.StatusCode, result);
            
        return Ok(result);
    }

    /// <summary>
    /// ID'ye göre sipariş getirir
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetOrderByIdAsync(id, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            _logger.LogWarning("Order with ID {OrderId} not found", id);
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Müşteri ID'sine göre siparişleri getirir
    /// </summary>
    [HttpGet("customer/{customerId}")]
    public async Task<IActionResult> GetOrdersByCustomerId(
        string customerId, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10, 
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetOrdersByCustomerIdAsync(customerId, page, pageSize, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            _logger.LogWarning("Error occurred while getting orders for customer {CustomerId}: {Error}", customerId, result.Message);
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Duruma göre siparişleri getirir
    /// </summary>
    [HttpGet("status/{status}")]
    public async Task<IActionResult> GetOrdersByStatus(
        OrderStatus status, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10, 
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetOrdersByStatusAsync(status, page, pageSize, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            _logger.LogWarning("Error occurred while getting orders with status {Status}: {Error}", status, result.Message);
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Yeni sipariş oluşturur
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _orderService.CreateOrderAsync(request, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            _logger.LogWarning("Error occurred while creating order: {Error}", result.Message);
            return StatusCode(result.StatusCode, result);
        }

        return CreatedAtAction(nameof(GetOrderById), new { id = result.Data.Id }, result);
    }

    /// <summary>
    /// Siparişi onayla
    /// </summary>
    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> ConfirmOrder(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _orderService.UpdateOrderStatusAsync(id, OrderStatus.Confirmed, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            if (result.StatusCode == 404)
                _logger.LogWarning("Order with ID {OrderId} not found for confirmation", id);
            else
                _logger.LogError("Error occurred while confirming order with ID {OrderId}: {Error}", id, result.Message);
                
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Siparişi iptal eder
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _orderService.CancelOrderAsync(id, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            if (result.StatusCode == 404)
                _logger.LogWarning("Order with ID {OrderId} not found for cancellation", id);
            else
                _logger.LogError("Error occurred while canceling order with ID {OrderId}: {Error}", id, result.Message);
                
            return StatusCode(result.StatusCode, result);
        }

        return NoContent();
    }

    /// <summary>
    /// Stok rezervasyonunu onaylar
    /// </summary>
    [HttpPost("{id:guid}/confirm-stock-reservation")]
    public async Task<IActionResult> ConfirmStockReservation(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _orderService.ConfirmStockReservationAsync(id, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            _logger.LogError("Error confirming stock reservation for order {OrderId}: {Error}", id, result.Message);
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Stok rezervasyonunu başarısız olarak işaretler
    /// </summary>
    [HttpPost("{id:guid}/fail-stock-reservation")]
    public async Task<IActionResult> FailStockReservation(Guid id, [FromBody] FailStockReservationRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _orderService.FailStockReservationAsync(id, request.Reason, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            _logger.LogError("Error failing stock reservation for order {OrderId}: {Error}", id, result.Message);
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Siparişi onaylar (stok rezervasyonu sonrası)
    /// </summary>
    [HttpPost("{id:guid}/confirm-order")]
    public async Task<IActionResult> ConfirmFinalOrder(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _orderService.ConfirmOrderAsync(id, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            _logger.LogError("Error confirming order {OrderId}: {Error}", id, result.Message);
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }
}

public class FailStockReservationRequest
{
    public string Reason { get; set; } = "Stock not available";
}