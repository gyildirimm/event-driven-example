using Microsoft.EntityFrameworkCore;
using OrderService.Application.Repositories;
using OrderService.Domain.Entities;
using OrderService.Persistence.Contexts;
using Shared.Kernel.Persistence.Repositories;

namespace OrderService.Persistence.Repositories;

public class OrderRepository(OrderContext context) : EfRepositoryBase<Order, Guid, OrderContext>(context), IOrderRepository
{
    public async Task<List<OutboxEvent>> GetUnprocessedOutboxEventsAsync(int maxEvents = 100)
    {
        return await Context.OutboxEvents
            .Where(e => !e.Processed && e.RetryCount < e.MaxRetries)
            .OrderBy(e => e.OccurredOn)
            .Take(maxEvents)
            .ToListAsync();
    }

    public async Task<OutboxEvent?> GetOutboxEventByIdAsync(Guid id)
    {
        return await Context.OutboxEvents.FindAsync(id);
    }

    public async Task UpdateOutboxEventAsync(OutboxEvent outboxEvent)
    {
        Context.OutboxEvents.Update(outboxEvent);
        await Context.SaveChangesAsync();
    }
}