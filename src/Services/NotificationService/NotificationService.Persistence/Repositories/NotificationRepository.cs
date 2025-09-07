using NotificationService.Application.Repositories;
using NotificationService.Domain.Entities;
using NotificationService.Persistence.Contexts;
using Shared.Kernel.Persistence.Repositories;

namespace NotificationService.Persistence.Repositories;

public class NotificationRepository(NotificationContext context) : EfRepositoryBase<Notification, Guid, NotificationContext>(context), INotificationRepository
{
    
}