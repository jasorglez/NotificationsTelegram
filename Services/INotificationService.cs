using NotificationsTelegram.DTOs;
using NotificationsTelegram.Models;

namespace NotificationsTelegram.Services;

public interface INotificationService
{
    /// <summary>
    /// Send a new notification for document authorization
    /// </summary>
    Task<NotificationResponse> SendNotificationAsync(SendNotificationRequest request);

    /// <summary>
    /// Get notification by ID
    /// </summary>
    Task<Notification?> GetByIdAsync(int id);

    /// <summary>
    /// Get notification status
    /// </summary>
    Task<NotificationStatusResponse?> GetStatusAsync(int id);

    /// <summary>
    /// Get pending notifications for a user (as authorizer)
    /// </summary>
    Task<List<PendingNotificationResponse>> GetPendingByAuthorizerAsync(int userId);

    /// <summary>
    /// Get notifications history
    /// </summary>
    Task<List<NotificationStatusResponse>> GetHistoryAsync(int? idSolicit = null, int? idAuthorize = null, string? status = null);

    /// <summary>
    /// Approve a notification
    /// </summary>
    Task<bool> ApproveAsync(int notificationId);

    /// <summary>
    /// Set notification to awaiting reason status
    /// </summary>
    Task<bool> SetAwaitingReasonAsync(int notificationId);

    /// <summary>
    /// Reject a notification with reason
    /// </summary>
    Task<bool> RejectWithReasonAsync(int notificationId, string reason);

    /// <summary>
    /// Get notification awaiting reason by chat ID
    /// </summary>
    Task<Notification?> GetAwaitingReasonByChatIdAsync(string chatId);

    /// <summary>
    /// Add log entry
    /// </summary>
    Task AddLogAsync(int notificationId, string action, string? detail = null);
}
