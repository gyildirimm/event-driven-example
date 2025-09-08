using Moq;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Models;
using NotificationService.Application.Repositories;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Persistence.Contexts;
using Shared.Kernel.Application.Repositories;
using System.Linq.Expressions;

namespace NotificationService.Test;

public class NotificationUnitTest
{
    [Fact]
    public void CreateEmailNotification_ShouldCreateValidNotification()
    {
        // Arrange
        var recipient = "test@example.com";
        var subject = "Test Subject";
        var body = "Test Body";

        // Act
        var notification = Notification.Email(recipient, subject, body);

        // Assert
        Assert.NotNull(notification);
        Assert.Equal(NotificationType.Email, notification.Channel);
        Assert.Equal(recipient, notification.Recipient);
        Assert.Equal(subject, notification.Subject);
        Assert.Equal(body, notification.Body);
        Assert.Equal(NotificationStatus.Created, notification.Status);
    }

    [Theory]
    [InlineData("test@example.com", "Test Subject", "Test Body")]
    [InlineData("user@domain.com", "Important Notice", "This is an important message")]
    [InlineData("admin@company.org", "System Alert", "System maintenance scheduled")]
    public void CreateEmailNotification_ShouldCreateValidNotification_WithDifferentInputs(string recipient, string subject, string body)
    {
        // Act
        var notification = Notification.Email(recipient, subject, body);

        // Assert
        Assert.NotNull(notification);
        Assert.Equal(NotificationType.Email, notification.Channel);
        Assert.Equal(recipient, notification.Recipient);
        Assert.Equal(subject, notification.Subject);
        Assert.Equal(body, notification.Body);
        Assert.Equal(NotificationStatus.Created, notification.Status);
    }

    [Fact]
    public void CreateSmsNotification_ShouldCreateValidNotification()
    {
        // Arrange
        var recipient = "+1234567890";
        var text = "Test SMS message";

        // Act
        var notification = Notification.Sms(recipient, text);

        // Assert
        Assert.NotNull(notification);
        Assert.Equal(NotificationType.Sms, notification.Channel);
        Assert.Equal(recipient, notification.Recipient);
        Assert.Equal(text, notification.Text);
        Assert.Equal(NotificationStatus.Created, notification.Status);
    }

    [Theory]
    [InlineData("+1234567890", "Test message")]
    [InlineData("+905551234567", "Türkçe mesaj")]
    [InlineData("+447911123456", "UK number test")]
    public void CreateSmsNotification_ShouldCreateValidNotification_WithDifferentInputs(string recipient, string text)
    {
        // Act
        var notification = Notification.Sms(recipient, text);

        // Assert
        Assert.NotNull(notification);
        Assert.Equal(NotificationType.Sms, notification.Channel);
        Assert.Equal(recipient, notification.Recipient);
        Assert.Equal(text, notification.Text);
        Assert.Equal(NotificationStatus.Created, notification.Status);
    }

    [Fact]
    public void NotificationStatusChanges_ShouldUpdateStatusCorrectly()
    {
        // Arrange
        var notification = Notification.Email("test@example.com", "Test", "Test");

        // Act & Assert - Test different status changes
        notification.MarkQueued();
        Assert.Equal(NotificationStatus.Queued, notification.Status);

        notification.MarkDelivered();
        Assert.Equal(NotificationStatus.Delivered, notification.Status);

        // Create a new notification for failed test
        var failedNotification = Notification.Email("test@example.com", "Test", "Test");
        failedNotification.MarkFailed("Test error");
        Assert.Equal(NotificationStatus.Failed, failedNotification.Status);
    }

    [Theory]
    [InlineData(NotificationStatus.Created)]
    [InlineData(NotificationStatus.Queued)]
    [InlineData(NotificationStatus.Delivered)]
    [InlineData(NotificationStatus.Failed)]
    [InlineData(NotificationStatus.Undeliverable)]
    public void NotificationStatus_ShouldBeSetCorrectly(NotificationStatus expectedStatus)
    {
        // Arrange
        var notification = Notification.Email("test@example.com", "Test", "Test");

        // Act
        switch (expectedStatus)
        {
            case NotificationStatus.Created:
                notification.MarkCreated();
                break;
            case NotificationStatus.Queued:
                notification.MarkQueued();
                break;
            case NotificationStatus.Delivered:
                notification.MarkDelivered();
                break;
            case NotificationStatus.Failed:
                notification.MarkFailed("Test error");
                break;
            case NotificationStatus.Undeliverable:
                notification.MarkUndeliverable("Test error");
                break;
        }

        // Assert
        Assert.Equal(expectedStatus, notification.Status);
    }

    [Fact]
    public void NotificationAttemptCount_ShouldIncrementCorrectly()
    {
        // Arrange
        var notification = Notification.Email("test@example.com", "Test", "Test");
        var initialCount = notification.AttemptCount;

        // Act
        notification.IncrementAttempt();

        // Assert
        Assert.Equal(initialCount + 1, notification.AttemptCount);
    }

    [Theory]
    [InlineData("test@example.com", "Subject", "Body", NotificationType.Email)]
    [InlineData("+1234567890", null, "SMS Text", NotificationType.Sms)]
    public void NotificationDto_ShouldMapCorrectly(string recipient, string? subject, string content, NotificationType type)
    {
        // Arrange
        var dto = new NotificationDto
        {
            Id = Guid.NewGuid(),
            Recipient = recipient,
            Subject = subject,
            Channel = type,
            Status = NotificationStatus.Created,
            CreatedAt = DateTime.UtcNow
        };

        if (type == NotificationType.Email)
        {
            dto.Body = content;
        }
        else
        {
            dto.Text = content;
        }

        // Assert
        Assert.Equal(recipient, dto.Recipient);
        Assert.Equal(subject, dto.Subject);
        Assert.Equal(type, dto.Channel);
        Assert.Equal(NotificationStatus.Created, dto.Status);
        
        if (type == NotificationType.Email)
        {
            Assert.Equal(content, dto.Body);
        }
        else
        {
            Assert.Equal(content, dto.Text);
        }
    }

    // Mock örneği - NotificationService ile
    [Fact]
    public async Task GetNotificationByIdAsync_ShouldReturnSuccess_WhenNotificationExists_MockExample()
    {
        // Arrange - Mock'ları oluştur
        var mockRepository = new Mock<INotificationRepository>();
        var mockLogger = new Mock<ILogger<NotificationService.Persistence.Services.NotificationService>>();
        var mockUnitOfWork = new Mock<IUnitOfWork<NotificationContext>>();

        // Test verileri
        var notificationId = Guid.NewGuid();
        var expectedNotification = Notification.Email("test@example.com", "Test Subject", "Test Body");
        
        // Mock repository'nin GetAsync metodunu setup et
        mockRepository.Setup(x => x.GetAsync(
                It.IsAny<Expression<Func<Notification, bool>>>(), 
                It.IsAny<Func<IQueryable<Notification>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Notification, object>>?>(),
                It.IsAny<bool>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedNotification);

        // NotificationService instance'ını mock'larla oluştur
        var notificationService = new NotificationService.Persistence.Services.NotificationService(
            mockRepository.Object,
            mockLogger.Object,
            mockUnitOfWork.Object);

        // Act
        var result = await notificationService.GetNotificationByIdAsync(notificationId);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Equal("test@example.com", result.Data.Recipient);
        Assert.Equal("Test Subject", result.Data.Subject);
        Assert.Equal("Test Body", result.Data.Body);
        
        mockRepository.Verify(x => x.GetAsync(
            It.IsAny<Expression<Func<Notification, bool>>>(),
            It.IsAny<Func<IQueryable<Notification>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Notification, object>>?>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetNotificationByIdAsync_ShouldReturnNotFound_WhenNotificationDoesNotExist_MockExample()
    {
        // Arrange
        var mockRepository = new Mock<INotificationRepository>();
        var mockLogger = new Mock<ILogger<NotificationService.Persistence.Services.NotificationService>>();
        var mockUnitOfWork = new Mock<IUnitOfWork<NotificationContext>>();

        var notificationId = Guid.NewGuid();
        
        // Repository null döndürecek şekilde setup et
        mockRepository.Setup(x => x.GetAsync(
                It.IsAny<Expression<Func<Notification, bool>>>(), 
                It.IsAny<Func<IQueryable<Notification>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Notification, object>>?>(),
                It.IsAny<bool>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification?)null);

        var notificationService = new NotificationService.Persistence.Services.NotificationService(
            mockRepository.Object,
            mockLogger.Object,
            mockUnitOfWork.Object);

        // Act
        var result = await notificationService.GetNotificationByIdAsync(notificationId);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal(404, result.StatusCode);
        Assert.Contains("not found", result.Message);
        
        mockRepository.Verify(x => x.GetAsync(
            It.IsAny<Expression<Func<Notification, bool>>>(),
            It.IsAny<Func<IQueryable<Notification>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Notification, object>>?>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}