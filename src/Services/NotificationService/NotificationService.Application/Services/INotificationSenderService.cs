using NotificationService.Application.Models.Events;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using Shared.Kernel.Application.OperationResults;

namespace NotificationService.Application.Services;

public interface INotificationSenderService
{
    Task<OperationResult> SendNotificationAsync(INotificationEvent notificationEvent, CancellationToken cancellationToken = default);
}