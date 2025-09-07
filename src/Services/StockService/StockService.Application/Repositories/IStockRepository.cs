using Shared.Kernel.Application.Repositories;
using StockService.Domain.Entities;

namespace StockService.Application.Repositories;

public interface IStockRepository : IQuery<Stock>, IAsyncRepository<Stock, Guid>
{
}