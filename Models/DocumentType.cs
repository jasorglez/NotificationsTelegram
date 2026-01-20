using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotificationsTelegram.Models;

[Table("DocumentTypes")]
public class DocumentType
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Microservice { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string BaseUrl { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string CallbackEndpoint { get; set; } = string.Empty;

    [StringLength(200)]
    public string? ViewUrl { get; set; }

    public bool Active { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
