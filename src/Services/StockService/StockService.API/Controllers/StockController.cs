using Microsoft.AspNetCore.Mvc;
using StockService.Application.DTOs;
using StockService.Application.Services;
using Shared.Kernel.Application.OperationResults;

namespace StockService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly IStockService _stockService;
    private readonly ILogger<StockController> _logger;

    public StockController(IStockService stockService, ILogger<StockController> logger)
    {
        _stockService = stockService ?? throw new ArgumentNullException(nameof(stockService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tüm stokları getirir
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetStocks([FromQuery] StockQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        var result = await _stockService.GetStocksAsync(queryParameters, cancellationToken);
        
        if (!result.IsSuccessful)
            return StatusCode(result.StatusCode, result);
            
        return Ok(result);
    }

    /// <summary>
    /// ID'ye göre stok getirir
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetStockById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _stockService.GetStockByIdAsync(id, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            _logger.LogWarning("Stock with ID {StockId} not found", id);
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Ürün ID'sine göre stok getirir
    /// </summary>
    [HttpGet("product/{productId:guid}")]
    public async Task<IActionResult> GetStockByProductId(Guid productId, CancellationToken cancellationToken = default)
    {
        var result = await _stockService.GetStockByProductIdAsync(productId, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            _logger.LogWarning("Stock for product {ProductId} not found", productId);
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Mevcut stokları getirir
    /// </summary>
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableStocks(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10, 
        CancellationToken cancellationToken = default)
    {
        var result = await _stockService.GetAvailableStocksAsync(page, pageSize, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            _logger.LogWarning("Error occurred while getting available stocks: {Error}", result.Message);
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Yeni stok oluşturur
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateStock([FromBody] CreateStockRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _stockService.CreateStockAsync(request, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            _logger.LogWarning("Error occurred while creating stock: {Error}", result.Message);
            return StatusCode(result.StatusCode, result);
        }

        return CreatedAtAction(nameof(GetStockById), new { id = result.Data.Id }, result);
    }

    /// <summary>
    /// Stok günceller
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateStock(Guid id, [FromBody] UpdateStockRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _stockService.UpdateStockAsync(id, request, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            if (result.StatusCode == 404)
                _logger.LogWarning("Stock with ID {StockId} not found for update", id);
            else
                _logger.LogError("Error occurred while updating stock with ID {StockId}: {Error}", id, result.Message);
                
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Stok miktarını günceller
    /// </summary>
    [HttpPatch("{id:guid}/quantity")]
    public async Task<IActionResult> UpdateStockQuantity(Guid id, [FromBody] int newQuantity, CancellationToken cancellationToken = default)
    {
        var result = await _stockService.UpdateStockQuantityAsync(id, newQuantity, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            if (result.StatusCode == 404)
                _logger.LogWarning("Stock with ID {StockId} not found for quantity update", id);
            else
                _logger.LogError("Error occurred while updating stock quantity for ID {StockId}: {Error}", id, result.Message);
                
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Stok miktarı ekler
    /// </summary>
    [HttpPatch("{id:guid}/add-quantity")]
    public async Task<IActionResult> AddStockQuantity(Guid id, [FromBody] int quantity, CancellationToken cancellationToken = default)
    {
        var result = await _stockService.AddStockQuantityAsync(id, quantity, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            if (result.StatusCode == 404)
                _logger.LogWarning("Stock with ID {StockId} not found for adding quantity", id);
            else
                _logger.LogError("Error occurred while adding stock quantity for ID {StockId}: {Error}", id, result.Message);
                
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Stok rezerve eder
    /// </summary>
    [HttpPost("reserve")]
    public async Task<IActionResult> ReserveStock([FromBody] ReserveStockRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _stockService.ReserveStockAsync(request, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            _logger.LogWarning("Error occurred while reserving stock for product {ProductId}: {Error}", request.ProductId, result.Message);
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Stok rezervasyonunu iptal eder
    /// </summary>
    [HttpPost("release-reservation")]
    public async Task<IActionResult> ReleaseReservation(
        [FromQuery] Guid productId, 
        [FromQuery] Guid orderId, 
        [FromQuery] int quantity, 
        CancellationToken cancellationToken = default)
    {
        var result = await _stockService.ReleaseReservationAsync(productId, orderId, quantity, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            _logger.LogWarning("Error occurred while releasing reservation for product {ProductId}, order {OrderId}: {Error}", 
                productId, orderId, result.Message);
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Stok rezervasyonunu onaylar
    /// </summary>
    [HttpPost("confirm-reservation")]
    public async Task<IActionResult> ConfirmReservation(
        [FromQuery] Guid productId, 
        [FromQuery] Guid orderId, 
        [FromQuery] int quantity, 
        CancellationToken cancellationToken = default)
    {
        var result = await _stockService.ConfirmReservationAsync(productId, orderId, quantity, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            _logger.LogWarning("Error occurred while confirming reservation for product {ProductId}, order {OrderId}: {Error}", 
                productId, orderId, result.Message);
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Stok müsaitlik kontrolü yapar
    /// </summary>
    [HttpGet("check-availability")]
    public async Task<IActionResult> CheckStockAvailability(
        [FromQuery] Guid productId, 
        [FromQuery] int requiredQuantity, 
        CancellationToken cancellationToken = default)
    {
        var result = await _stockService.CheckStockAvailabilityAsync(productId, requiredQuantity, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            _logger.LogWarning("Error occurred while checking stock availability for product {ProductId}: {Error}", 
                productId, result.Message);
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Mevcut stok miktarını getirir
    /// </summary>
    [HttpGet("available-quantity/{productId:guid}")]
    public async Task<IActionResult> GetAvailableQuantity(Guid productId, CancellationToken cancellationToken = default)
    {
        var result = await _stockService.GetAvailableQuantityAsync(productId, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            _logger.LogWarning("Error occurred while getting available quantity for product {ProductId}: {Error}", 
                productId, result.Message);
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Stok doğrulama yapar
    /// </summary>
    [HttpGet("{id:guid}/validate")]
    public async Task<IActionResult> ValidateStock(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _stockService.ValidateStockAsync(id, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            _logger.LogWarning("Error occurred while validating stock {StockId}: {Error}", id, result.Message);
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }
}