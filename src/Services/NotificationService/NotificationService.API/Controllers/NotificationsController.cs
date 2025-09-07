using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Models;
using NotificationService.Application.Services;
using Shared.Kernel.Application.OperationResults;

namespace NotificationService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Tüm bildirimleri getirir
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] NotificationQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Get notifications failed due to validation errors");
            return BadRequest(ModelState);
        }

        var result = await _notificationService.GetNotificationsAsync(queryParameters, cancellationToken);
        
        if (!result.IsSuccessful)
            return StatusCode(result.StatusCode, result);
            
        return Ok(result);
    }

    /// <summary>
    /// ID'ye göre bildirim getirir
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetNotificationById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _notificationService.GetNotificationByIdAsync(id, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            _logger.LogWarning("Notification with ID {NotificationId} not found", id);
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Alıcıya göre bildirimleri getirir
    /// </summary>
    [HttpGet("recipient/{recipient}")]
    public async Task<IActionResult> GetNotificationsByRecipient(
        string recipient, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10, 
        CancellationToken cancellationToken = default)
    {
        var result = await _notificationService.GetNotificationsByRecipientAsync(recipient, page, pageSize, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            _logger.LogWarning("Error occurred while getting notifications for recipient {Recipient}: {Error}", recipient, result.Message);
            return StatusCode(result.StatusCode, result);
        }

        return Ok(result);
    }

    /// <summary>
    /// SMS bildirimi oluşturur
    /// </summary>
    [HttpPost("sms")]
    public async Task<IActionResult> CreateSmsNotification([FromBody] NotificationSmsCreateModel request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("SMS notification creation failed due to validation errors");
            return BadRequest(ModelState);
        }

        var result = await _notificationService.CreateSmsAsync(request, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            _logger.LogWarning("Error occurred while creating SMS notification: {Error}", result.Message);
            return StatusCode(result.StatusCode, result);
        }

        return CreatedAtAction(nameof(GetNotificationById), new { id = result.Data.Id }, result);
    }

    /// <summary>
    /// Email bildirimi oluşturur
    /// </summary>
    [HttpPost("email")]
    public async Task<IActionResult> CreateEmailNotification([FromBody] NotificationEmailCreateModel request, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Email notification creation failed due to validation errors");
            return BadRequest(ModelState);
        }

        var result = await _notificationService.CreateEmailAsync(request, cancellationToken);
        
        if (!result.IsSuccessful)
        {
            _logger.LogWarning("Error occurred while creating email notification: {Error}", result.Message);
            return StatusCode(result.StatusCode, result);
        }

        return CreatedAtAction(nameof(GetNotificationById), new { id = result.Data.Id }, result);
    }
}