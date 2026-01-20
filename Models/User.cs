using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotificationsTelegram.Models;

[Table("Users")]
public class User
{
    [Key]
    public int Id { get; set; }

    [StringLength(100)]
    public string? DisplayName { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [Column("idTelegram")]
    [StringLength(20)]
    public string? IdTelegram { get; set; }

    [Column("allowWhatsapp")]
    public bool? AllowWhatsapp { get; set; }

    public int? Active { get; set; }
}
