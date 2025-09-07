using Shared.Kernel.Persistence.Repositories;
using StockService.Application.Repositories;
using StockService.Domain.Entities;
using StockService.Persistence.Contexts;

namespace StockService.Persistence.Repositories;

public class StockRepository(StockContext context)
    : EfRepositoryBase<Stock, Guid, StockContext>(context), IStockRepository;