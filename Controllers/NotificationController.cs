using Microsoft.AspNetCore.Mvc;
using NotificationsTelegram.DTOs;
using NotificationsTelegram.Services;

namespace NotificationsTelegram.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        INotificationService notificationService,
        ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Send a new notification for document authorization
    /// </summary>
    [HttpPost("send")]
    public async Task<ActionResult<NotificationResponse>> Send([FromBody] SendNotificationRequest request)
    {
        _logger.LogInformation("Received notification request for {DocumentType} {Folio}",
            request.DocumentType, request.Folio);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _notificationService.SendNotificationAsync(request);

        if (result.Success)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    /// <summary>
    /// Get notification status by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<NotificationStatusResponse>> GetById(int id)
    {
        var notification = await _notificationService.GetStatusAsync(id);

        if (notification == null)
        {
            return NotFound(new { message = $"Notification {id} not found" });
        }

        return Ok(notification);
    }

    /// <summary>
    /// Get pending notifications for a user (as authorizer)
    /// </summary>
    [HttpGet("pending/{userId}")]
    public async Task<ActionResult<List<PendingNotificationResponse>>> GetPending(int userId)
    {
        var notifications = await _notificationService.GetPendingByAuthorizerAsync(userId);
        return Ok(notifications);
    }

    /// <summary>
    /// Get notifications history
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<List<NotificationStatusResponse>>> GetHistory(
        [FromQuery] int? idSolicit,
        [FromQuery] int? idAuthorize,
        [FromQuery] string? status)
    {
        var notifications = await _notificationService.GetHistoryAsync(idSolicit, idAuthorize, status);
        return Ok(notifications);
    }
}
