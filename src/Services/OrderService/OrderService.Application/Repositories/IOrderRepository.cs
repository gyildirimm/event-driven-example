using OrderService.Domain.Entities;
using Shared.Kernel.Application.Repositories;

namespace OrderService.Application.Repositories;

public interface IOrderRepository : IQuery<Order>, IAsyncRepository<Order, Guid>
{
    Task<List<OutboxEvent>> GetUnprocessedOutboxEventsAsync(int maxEvents = 100);
    Task<OutboxEvent?> GetOutboxEventByIdAsync(Guid id);
    Task UpdateOutboxEventAsync(OutboxEvent outboxEvent);
}