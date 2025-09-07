using NotificationService.Application.Models;
using NotificationService.Domain.Enums;
using Shared.Kernel.Application.OperationResults;
using Shared.Kernel.Application.OperationResults.Paging;

namespace NotificationService.Application.Services;

public interface INotificationService
{
    Task<OperationResult<NotificationDto>> CreateSmsAsync(NotificationSmsCreateModel request, CancellationToken cancellationToken = default);
    Task<OperationResult<NotificationDto>> CreateEmailAsync(NotificationEmailCreateModel request, CancellationToken cancellationToken = default);

    Task<OperationResult<NotificationDto>> GetNotificationByIdAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task<OperationResult<IPaginate<NotificationDto>>> GetNotificationsByRecipientAsync(string recipient, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<OperationResult<IPaginate<NotificationDto>>> GetNotificationsAsync(NotificationQueryParameters queryParameters, CancellationToken cancellationToken = default);
    
    Task<OperationResult> UpdateNotificationStatusAsync(Guid notificationId, NotificationStatus status, CancellationToken cancellationToken = default);
}