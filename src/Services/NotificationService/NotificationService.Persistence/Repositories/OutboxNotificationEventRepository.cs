using NotificationService.Application.Repositories;
using NotificationService.Domain.Entities;
using NotificationService.Persistence.Contexts;
using Shared.Kernel.Persistence.Repositories;

namespace NotificationService.Persistence.Repositories;

public class OutboxNotificationEventRepository(NotificationContext context) : EfRepositoryBase<OutboxNotificationEvent, Guid, NotificationContext>(context), IOutboxNotificationEventRepository
{
    
}