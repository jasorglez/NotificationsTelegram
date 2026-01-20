using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotificationsTelegram.Models;

[Table("Notifications")]
public class Notification
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int IdDocumentType { get; set; }

    [Required]
    public int DocumentId { get; set; }

    [Required]
    [StringLength(50)]
    public string Folio { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    [Required]
    public int IdSolicit { get; set; }

    [Required]
    public int IdAuthorize { get; set; }

    public long? TelegramMsgId { get; set; }

    public long? TelegramChatId { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = NotificationStatus.PENDING;

    [StringLength(500)]
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? SentAt { get; set; }

    public DateTime? RespondedAt { get; set; }

    public bool CallbackSent { get; set; } = false;

    public bool SolicitNotified { get; set; } = false;

    public bool Active { get; set; } = true;

    // Navigation
    [ForeignKey("IdDocumentType")]
    public virtual DocumentType? DocumentType { get; set; }

    public virtual ICollection<NotificationLog> Logs { get; set; } = new List<NotificationLog>();
}

// Status constants
public static class NotificationStatus
{
    public const string PENDING = "PENDING";
    public const string APPROVED = "APPROVED";
    public const string REJECTED = "REJECTED";
    public const string AWAITING_REASON = "AWAITING_REASON";
    public const string ERROR = "ERROR";
}
