namespace NotificationsTelegram.DTOs;

/// <summary>
/// Response after sending a notification
/// </summary>
public class NotificationResponse
{
    public bool Success { get; set; }
    public int? NotificationId { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Notification status response
/// </summary>
public class NotificationStatusResponse
{
    public int Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? SolicitName { get; set; }
    public string? AuthorizeName { get; set; }
}

/// <summary>
/// Pending notifications for a user
/// </summary>
public class PendingNotificationResponse
{
    public int Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentTypeDescription { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SolicitName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ViewUrl { get; set; }
}
