using StockService.Application.DTOs;
using StockService.Application.Services;
using StockService.Domain.Entities;
using StockService.Persistence.Contexts;
using Shared.Kernel.Application.OperationResults;
using Shared.Kernel.Application.OperationResults.Paging;
using Shared.Kernel.Application.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace StockService.Persistence.Services;

public class StockApplicationService(IUnitOfWork<StockContext> unitOfWork) : IStockService
{
    private readonly IUnitOfWork<StockContext> _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task<OperationResult<StockDto>> CreateStockAsync(CreateStockRequest request, CancellationToken cancellationToken = default)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var repository = _unitOfWork.GetAsyncRepository<Stock, Guid>();
            
            // Check if stock already exists for this product
            var existingStock = await repository.GetAsync(s => s.ProductId == request.ProductId, cancellationToken: cancellationToken);
            if (existingStock != null)
            {
                return OperationResult<StockDto>.Fail("Stock already exists for this product", statusCode: 409);
            }

            var stock = new Stock(request.ProductId, request.Quantity);
            await repository.AddAsync(stock);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(transaction, cancellationToken);

            var stockDto = MapToDto(stock);
            return OperationResult<StockDto>.Success(stockDto, "Stock created successfully", 201);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction, cancellationToken);
            return OperationResult<StockDto>.Fail($"Error creating stock: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult<StockDto>> GetStockByIdAsync(Guid stockId, CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _unitOfWork.GetAsyncRepository<Stock, Guid>();
            var stock = await repository.GetAsync(s => s.Id == stockId, cancellationToken: cancellationToken);

            if (stock == null)
                return OperationResult<StockDto>.Fail("Stock not found", statusCode: 404);

            var stockDto = MapToDto(stock);
            return OperationResult<StockDto>.Success(stockDto);
        }
        catch (Exception ex)
        {
            return OperationResult<StockDto>.Fail($"Error retrieving stock: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult<StockDto>> GetStockByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _unitOfWork.GetAsyncRepository<Stock, Guid>();
            var stock = await repository.GetAsync(s => s.ProductId == productId, cancellationToken: cancellationToken);

            if (stock == null)
                return OperationResult<StockDto>.Fail("Stock not found for this product", statusCode: 404);

            var stockDto = MapToDto(stock);
            return OperationResult<StockDto>.Success(stockDto);
        }
        catch (Exception ex)
        {
            return OperationResult<StockDto>.Fail($"Error retrieving stock: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult<IPaginate<StockDto>>> GetStocksAsync(StockQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _unitOfWork.GetAsyncRepository<Stock, Guid>();
            
            // Build predicate step by step
            Expression<Func<Stock, bool>>? predicate = null;
            
            if (queryParameters.ProductId.HasValue)
            {
                predicate = s => s.ProductId == queryParameters.ProductId.Value;
            }
            
            // For now, use simple approach without complex filtering
            var result = await repository.GetListAsync(
                predicate: predicate,
                index: queryParameters.Page - 1,
                size: queryParameters.PageSize,
                cancellationToken: cancellationToken);

            var paginatedResult = Paginate.From(result, items => items.Select(MapToDto));
            return OperationResult<IPaginate<StockDto>>.Success(paginatedResult);
        }
        catch (Exception ex)
        {
            return OperationResult<IPaginate<StockDto>>.Fail($"Error retrieving stocks: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult<IPaginate<StockDto>>> GetAvailableStocksAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _unitOfWork.GetAsyncRepository<Stock, Guid>();
            var result = await repository.GetListAsync(
                predicate: s => s.AvailableQuantity > 0,
                index: page - 1,
                size: pageSize,
                cancellationToken: cancellationToken);

            var paginatedResult = Paginate.From(result, items => items.Select(MapToDto));
            return OperationResult<IPaginate<StockDto>>.Success(paginatedResult);
        }
        catch (Exception ex)
        {
            return OperationResult<IPaginate<StockDto>>.Fail($"Error retrieving available stocks: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult<StockDto>> UpdateStockAsync(Guid stockId, UpdateStockRequest request, CancellationToken cancellationToken = default)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var repository = _unitOfWork.GetAsyncRepository<Stock, Guid>();
            var stock = await repository.GetAsync(s => s.Id == stockId, cancellationToken: cancellationToken);

            if (stock == null)
                return OperationResult<StockDto>.Fail("Stock not found", statusCode: 404);

            stock.UpdateQuantity(request.Quantity);
            await repository.UpdateAsync(stock);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(transaction, cancellationToken);

            var stockDto = MapToDto(stock);
            return OperationResult<StockDto>.Success(stockDto, "Stock updated successfully");
        }
        catch (InvalidOperationException ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction, cancellationToken);
            return OperationResult<StockDto>.Fail(ex.Message, statusCode: 400);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction, cancellationToken);
            return OperationResult<StockDto>.Fail($"Error updating stock: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult<StockDto>> UpdateStockQuantityAsync(Guid stockId, int newQuantity, CancellationToken cancellationToken = default)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var repository = _unitOfWork.GetAsyncRepository<Stock, Guid>();
            var stock = await repository.GetAsync(s => s.Id == stockId, cancellationToken: cancellationToken);

            if (stock == null)
                return OperationResult<StockDto>.Fail("Stock not found", statusCode: 404);

            stock.UpdateQuantity(newQuantity);
            await repository.UpdateAsync(stock);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(transaction, cancellationToken);

            var stockDto = MapToDto(stock);
            return OperationResult<StockDto>.Success(stockDto, "Stock quantity updated successfully");
        }
        catch (InvalidOperationException ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction, cancellationToken);
            return OperationResult<StockDto>.Fail(ex.Message, statusCode: 400);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction, cancellationToken);
            return OperationResult<StockDto>.Fail($"Error updating stock quantity: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult<StockDto>> AddStockQuantityAsync(Guid stockId, int quantity, CancellationToken cancellationToken = default)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var repository = _unitOfWork.GetAsyncRepository<Stock, Guid>();
            var stock = await repository.GetAsync(s => s.Id == stockId, cancellationToken: cancellationToken);

            if (stock == null)
                return OperationResult<StockDto>.Fail("Stock not found", statusCode: 404);

            stock.UpdateQuantity(stock.Quantity + quantity);
            await repository.UpdateAsync(stock);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(transaction, cancellationToken);

            var stockDto = MapToDto(stock);
            return OperationResult<StockDto>.Success(stockDto, "Stock quantity added successfully");
        }
        catch (InvalidOperationException ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction, cancellationToken);
            return OperationResult<StockDto>.Fail(ex.Message, statusCode: 400);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction, cancellationToken);
            return OperationResult<StockDto>.Fail($"Error adding stock quantity: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult<StockReservationDto>> ReserveStockAsync(ReserveStockRequest request, CancellationToken cancellationToken = default)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var repository = _unitOfWork.GetAsyncRepository<Stock, Guid>();
            var stock = await repository.GetAsync(s => s.ProductId == request.ProductId, cancellationToken: cancellationToken);
            
            if (stock == null)
                return OperationResult<StockReservationDto>.Fail("Stock not found for this product", statusCode: 404);

            if (!stock.CanReserve(request.Quantity))
                return OperationResult<StockReservationDto>.Fail("Insufficient stock available", statusCode: 400);

            var reserved = stock.ReserveStock(request.Quantity);
            if (!reserved)
                return OperationResult<StockReservationDto>.Fail("Failed to reserve stock", statusCode: 400);

            await repository.UpdateAsync(stock);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(transaction, cancellationToken);

            var reservationDto = new StockReservationDto
            {
                Id = Guid.NewGuid(),
                ProductId = request.ProductId,
                OrderId = request.OrderId,
                ReservedQuantity = request.Quantity,
                ReservationDate = DateTime.UtcNow,
                IsConfirmed = false
            };

            return OperationResult<StockReservationDto>.Success(reservationDto, "Stock reserved successfully");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction, cancellationToken);
            return OperationResult<StockReservationDto>.Fail($"Error reserving stock: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult> ReleaseReservationAsync(Guid productId, Guid orderId, int quantity, CancellationToken cancellationToken = default)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var repository = _unitOfWork.GetAsyncRepository<Stock, Guid>();
            var stock = await repository.GetAsync(s => s.ProductId == productId, cancellationToken: cancellationToken);

            if (stock == null)
                return OperationResult.Fail("Stock not found for this product", statusCode: 404);

            var released = stock.ReleaseReservation(quantity);
            if (!released)
                return OperationResult.Fail("Failed to release reservation", statusCode: 400);

            await repository.UpdateAsync(stock);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(transaction, cancellationToken);

            return OperationResult.Success("Reservation released successfully");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction, cancellationToken);
            return OperationResult.Fail($"Error releasing reservation: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult> ConfirmReservationAsync(Guid productId, Guid orderId, int quantity, CancellationToken cancellationToken = default)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var repository = _unitOfWork.GetAsyncRepository<Stock, Guid>();
            var stock = await repository.GetAsync(s => s.ProductId == productId, cancellationToken: cancellationToken);

            if (stock == null)
                return OperationResult.Fail("Stock not found for this product", statusCode: 404);

            var confirmed = stock.ConfirmReservation(quantity);
            if (!confirmed)
                return OperationResult.Fail("Failed to confirm reservation", statusCode: 400);

            await repository.UpdateAsync(stock);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(transaction, cancellationToken);

            return OperationResult.Success("Reservation confirmed successfully");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(transaction, cancellationToken);
            return OperationResult.Fail($"Error confirming reservation: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult<bool>> CheckStockAvailabilityAsync(Guid productId, int requiredQuantity, CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _unitOfWork.GetAsyncRepository<Stock, Guid>();
            var stock = await repository.GetAsync(s => s.ProductId == productId, cancellationToken: cancellationToken);

            if (stock == null)
                return OperationResult<bool>.Fail("Stock not found for this product", statusCode: 404);

            var isAvailable = stock.CanReserve(requiredQuantity);
            return OperationResult<bool>.Success(isAvailable);
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.Fail($"Error checking stock availability: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult<int>> GetAvailableQuantityAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _unitOfWork.GetAsyncRepository<Stock, Guid>();
            var stock = await repository.GetAsync(s => s.ProductId == productId, cancellationToken: cancellationToken);

            if (stock == null)
                return OperationResult<int>.Fail("Stock not found for this product", statusCode: 404);

            return OperationResult<int>.Success(stock.AvailableQuantity);
        }
        catch (Exception ex)
        {
            return OperationResult<int>.Fail($"Error getting available quantity: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult<bool>> ValidateStockAsync(Guid stockId, CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = _unitOfWork.GetAsyncRepository<Stock, Guid>();
            var stock = await repository.GetAsync(s => s.Id == stockId, cancellationToken: cancellationToken);

            if (stock == null)
                return OperationResult<bool>.Fail("Stock not found", statusCode: 404);

            var isValid = stock.Quantity >= 0 && 
                         stock.ReservedQuantity >= 0 && 
                         stock.ReservedQuantity <= stock.Quantity;

            return OperationResult<bool>.Success(isValid);
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.Fail($"Error validating stock: {ex.Message}", statusCode: 500);
        }
    }

    private static StockDto MapToDto(Stock stock)
    {
        return new StockDto
        {
            Id = stock.Id,
            ProductId = stock.ProductId,
            Quantity = stock.Quantity,
            ReservedQuantity = stock.ReservedQuantity,
            AvailableQuantity = stock.AvailableQuantity,
            CreatedAt = stock.CreatedAt,
            UpdatedAt = stock.UpdatedAt
        };
    }
}
