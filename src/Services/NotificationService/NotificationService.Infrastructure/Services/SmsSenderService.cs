using Microsoft.Extensions.Logging;
using NotificationService.Application.Models;
using NotificationService.Application.Models.Events;
using NotificationService.Application.Services;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using Shared.Kernel.Application.OperationResults;
using Shared.Kernel.Application.Repositories;

namespace NotificationService.Infrastructure.Services;

public class SmsSenderService(INotificationService notificationService, ILogger<SmsSenderService> logger, IUnitOfWork unitOfWork) : INotificationSenderService
{
    private readonly INotificationService _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    private readonly ILogger<SmsSenderService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    
    public async Task<OperationResult> SendNotificationAsync(INotificationEvent notificationEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            OperationResult<NotificationDto>? notificationResult = await _notificationService.GetNotificationByIdAsync(notificationEvent.NotificationId, cancellationToken);
            if(!CheckCommonConditions(notificationResult))
            {
                _logger.LogWarning("Notification with ID {NotificationId} not found or is not of type SMS", notificationEvent.NotificationId);
                return OperationResult.Fail("Notification not found or invalid type");
            }

            var resultUpdateNotification = await _notificationService.UpdateNotificationStatusAsync(notificationEvent.NotificationId,
                NotificationStatus.Delivered, cancellationToken);
            
            if(!resultUpdateNotification.IsSuccessful)
                return OperationResult.Fail("Failed to update notification status");
        
            //SEND SMS LOGIC HERE
            Console.WriteLine($"Sending SMS to {notificationEvent.Recipient}: {notificationEvent.Content}");
        
            return OperationResult.Success("SMS sent successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending SMS: {ex.Message}");
            _logger.LogError(ex, "Error sending SMS for Notification ID {NotificationId}", notificationEvent.NotificationId);
            return OperationResult.Fail("SMS sent successfully");
        }
    }

    private bool CheckCommonConditions(OperationResult<NotificationDto> notificationResult)
    {
        if(!notificationResult.IsSuccessful || notificationResult.Data is null || notificationResult.Data?.Channel != NotificationType.Sms || notificationResult.Data.Status == NotificationStatus.Delivered)
        {
            return false;
        }

        return true;
    }
}