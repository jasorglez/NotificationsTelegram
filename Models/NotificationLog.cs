using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotificationsTelegram.Models;

[Table("NotificationLogs")]
public class NotificationLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int IdNotification { get; set; }

    [Required]
    [StringLength(50)]
    public string Action { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Detail { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    [ForeignKey("IdNotification")]
    public virtual Notification? Notification { get; set; }
}

// Action constants
public static class LogAction
{
    public const string CREATED = "CREATED";
    public const string SENT = "SENT";
    public const string DELIVERED = "DELIVERED";
    public const string APPROVED = "APPROVED";
    public const string REJECTED = "REJECTED";
    public const string CALLBACK_SENT = "CALLBACK_SENT";
    public const string CALLBACK_FAILED = "CALLBACK_FAILED";
    public const string SOLICIT_NOTIFIED = "SOLICIT_NOTIFIED";
    public const string ERROR = "ERROR";
}
