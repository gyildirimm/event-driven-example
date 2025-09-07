using StockService.Application.DTOs;
using Shared.Kernel.Application.OperationResults;
using Shared.Kernel.Application.OperationResults.Paging;

namespace StockService.Application.Services;

public interface IStockService
{
    // Create operations
    Task<OperationResult<StockDto>> CreateStockAsync(CreateStockRequest request, CancellationToken cancellationToken = default);
    
    // Read operations
    Task<OperationResult<StockDto>> GetStockByIdAsync(Guid stockId, CancellationToken cancellationToken = default);
    Task<OperationResult<StockDto>> GetStockByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<OperationResult<IPaginate<StockDto>>> GetStocksAsync(StockQueryParameters queryParameters, CancellationToken cancellationToken = default);
    Task<OperationResult<IPaginate<StockDto>>> GetAvailableStocksAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    
    // Update operations
    Task<OperationResult<StockDto>> UpdateStockAsync(Guid stockId, UpdateStockRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult<StockDto>> UpdateStockQuantityAsync(Guid stockId, int newQuantity, CancellationToken cancellationToken = default);
    Task<OperationResult<StockDto>> AddStockQuantityAsync(Guid stockId, int quantity, CancellationToken cancellationToken = default);
    
    // Stock reservation operations
    Task<OperationResult<StockReservationDto>> ReserveStockAsync(ReserveStockRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> ReleaseReservationAsync(Guid productId, Guid orderId, int quantity, CancellationToken cancellationToken = default);
    Task<OperationResult> ConfirmReservationAsync(Guid productId, Guid orderId, int quantity, CancellationToken cancellationToken = default);
    
    // Business operations
    Task<OperationResult<bool>> CheckStockAvailabilityAsync(Guid productId, int requiredQuantity, CancellationToken cancellationToken = default);
    Task<OperationResult<int>> GetAvailableQuantityAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<OperationResult<bool>> ValidateStockAsync(Guid stockId, CancellationToken cancellationToken = default);
}