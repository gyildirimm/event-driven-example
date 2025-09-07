using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Models;
using NotificationService.Application.Models.Events;
using NotificationService.Application.Repositories;
using NotificationService.Application.Services;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Persistence.Contexts;
using Shared.Kernel.Application.OperationResults;
using Shared.Kernel.Application.OperationResults.Paging;
using Shared.Kernel.Application.Repositories;

namespace NotificationService.Persistence.Services;

public class NotificationService(INotificationRepository repository, ILogger<NotificationService> logger, IUnitOfWork<NotificationContext> unitOfWork): INotificationService
{
    private readonly INotificationRepository _notificationRepository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly ILogger<NotificationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IUnitOfWork<NotificationContext> _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    
    public async Task<OperationResult<NotificationDto>> CreateSmsAsync(NotificationSmsCreateModel request, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var notification = Notification.Sms(request.Recipient, request.Text);
            await _notificationRepository.AddAsync(notification);
            
            var outboxRepository = _unitOfWork.GetAsyncRepository<OutboxNotificationEvent, Guid>();
            SmsNotificationEvent eventModel = new(notification.Id, notification.Recipient, notification.Subject ?? string.Empty, notification.Text!);
            
            var eventData = JsonSerializer.Serialize(eventModel);
            var outboxEvent = new OutboxNotificationEvent("notification.sms", eventData, "notification-events");
            
            await outboxRepository.AddAsync(outboxEvent);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(transaction, cancellationToken);
            
            var notificationDto = MapToDto(notification);
            return OperationResult<NotificationDto>.Success(notificationDto, "SMS created successfully", 201);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating SMS notification for recipient: {Recipient}", request.Recipient);
            
            await _unitOfWork.RollbackTransactionAsync(transaction, cancellationToken);
            return OperationResult<NotificationDto>.Fail($"Error creating SMS: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult<NotificationDto>> CreateEmailAsync(NotificationEmailCreateModel request, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var notification = Notification.Email(request.Recipient, request.Subject, request.Body);
            await _notificationRepository.AddAsync(notification);
            
            var outboxRepository = _unitOfWork.GetAsyncRepository<OutboxNotificationEvent, Guid>();
            EmailNotificationEvent eventModel = new(notification.Id, notification.Recipient, notification.Subject!, notification.Body!);
            
            var eventData = JsonSerializer.Serialize(eventModel);
            var outboxEvent = new OutboxNotificationEvent("notification.email", eventData, "notification-events");
            
            await outboxRepository.AddAsync(outboxEvent);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(transaction, cancellationToken);
            
            var notificationDto = MapToDto(notification);
            return OperationResult<NotificationDto>.Success(notificationDto, "EMAIL created successfully", 201);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating email notification for recipient: {Recipient}", request.Recipient);
            
            await _unitOfWork.RollbackTransactionAsync(transaction, cancellationToken);
            return OperationResult<NotificationDto>.Fail($"Error creating EMAIL: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult<NotificationDto>> GetNotificationByIdAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _notificationRepository.GetAsync(n => n.Id == notificationId, cancellationToken: cancellationToken);
            if (notification == null)
                return OperationResult<NotificationDto>.Fail($"Order with ID {notificationId} not found", statusCode: 404);
            
            var notificationDto = MapToDto(notification);
            return OperationResult<NotificationDto>.Success(notificationDto, "Notification retrieved successfully", 200);
        }
        catch (Exception ex)
        {
            return OperationResult<NotificationDto>.Fail($"Error retrieving Notification: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult<IPaginate<NotificationDto>>> GetNotificationsByRecipientAsync(string recipient, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await repository.GetListAsync(
                predicate: o => o.Recipient == recipient,
                index: page - 1,
                size: pageSize,
                cancellationToken: cancellationToken);
            
            var paginatedResult = Paginate.From(result, items => items.Select(MapToDto));
            return OperationResult<IPaginate<NotificationDto>>.Success(paginatedResult, "Recipient Notifications retrieved successfully");
        }
        catch (Exception ex)
        {
            return OperationResult<IPaginate<NotificationDto>>.Fail($"Error retrieving Recipient Notifications: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult<IPaginate<NotificationDto>>> GetNotificationsAsync(NotificationQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var predicate = Build(queryParameters);
            var result = await repository.GetListAsync(
                predicate: predicate,
                index: queryParameters.Page - 1,
                size: queryParameters.PageSize,
                cancellationToken: cancellationToken);
            
            var paginatedResult = Paginate.From(result, items => items.Select(MapToDto));
            return OperationResult<IPaginate<NotificationDto>>.Success(paginatedResult, "Notifications retrieved successfully");
        }
        catch (Exception ex)
        {
            return OperationResult<IPaginate<NotificationDto>>.Fail($"Error retrieving Notifications: {ex.Message}", statusCode: 500);
        }
    }

    public async Task<OperationResult> UpdateNotificationStatusAsync(Guid notificationId, NotificationStatus status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _notificationRepository.GetAsync(n => n.Id == notificationId, cancellationToken: cancellationToken);
            if (notification == null)
                return OperationResult.Fail($"Order with ID {notificationId} not found", statusCode: 404);

            switch (status)
            {
                case NotificationStatus.Created:
                    notification.MarkCreated();
                    break;
                case NotificationStatus.Delivered:
                    notification.MarkDelivered();
                    break;
                case NotificationStatus.Failed:
                    notification.MarkFailed("Marked as Failed via UpdateNotificationStatusAsync");
                    break;
                case NotificationStatus.Queued:
                    notification.MarkQueued();
                    break;
                case NotificationStatus.Undeliverable:
                    notification.MarkUndeliverable("Marked as Undeliverable via UpdateNotificationStatusAsync");
                    break;
            }
            
            await _notificationRepository.UpdateAsync(notification);

            var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            if (result == 0)
                return OperationResult.Fail("No changes were made to the notification", statusCode: 500);
            
            return OperationResult.Success("Notification retrieved successfully", 200);
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Error retrieving Notification: {ex.Message}", statusCode: 500);
        }
    }

    private Expression<Func<Notification, bool>> Build(NotificationQueryParameters parameters)
    {
        return n =>
            (string.IsNullOrEmpty(parameters.Recipient) || n.Recipient == parameters.Recipient) &&
            (!parameters.Status.HasValue || n.Status == parameters.Status.Value) &&
            (!parameters.Channel.HasValue || n.Channel == parameters.Channel.Value) &&
            (!parameters.StartDate.HasValue || n.CreatedAt >= parameters.StartDate.Value) &&
            (!parameters.EndDate.HasValue || n.CreatedAt <= parameters.EndDate.Value);
    }
    
    private static NotificationDto MapToDto(Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            Recipient = notification.Recipient,
            Subject = notification.Subject,
            Body = notification.Body,
            Text = notification.Text,
            AttemptCount = notification.AttemptCount,
            LastError = notification.LastError,
            SentAtUtc = notification.SentAtUtc,
            Status = notification.Status,
            CreatedAt = notification.CreatedAt,
            UpdatedAt = notification.UpdatedAt,
            Channel = notification.Channel,
        };
    }
}