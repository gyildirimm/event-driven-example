using NotificationService.Domain.Entities;
using Shared.Kernel.Application.Repositories;

namespace NotificationService.Application.Repositories;

public interface IOutboxNotificationEventRepository : IQuery<OutboxNotificationEvent>, IAsyncRepository<OutboxNotificationEvent, Guid>
{
    
}