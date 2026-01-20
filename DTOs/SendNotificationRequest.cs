using System.ComponentModel.DataAnnotations;

namespace NotificationsTelegram.DTOs;

/// <summary>
/// Request to send a notification for document authorization
/// </summary>
public class SendNotificationRequest
{
    /// <summary>
    /// Document type code (OC, REQ, EST, FAC, etc.)
    /// </summary>
    [Required]
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// Document ID in the origin microservice
    /// </summary>
    [Required]
    public int DocumentId { get; set; }

    /// <summary>
    /// Document folio/number for display
    /// </summary>
    [Required]
    public string Folio { get; set; } = string.Empty;

    /// <summary>
    /// Short description of the document
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// User ID who is requesting authorization
    /// </summary>
    [Required]
    public int IdSolicit { get; set; }

    /// <summary>
    /// User ID who should authorize
    /// </summary>
    [Required]
    public int IdAuthorize { get; set; }
}
