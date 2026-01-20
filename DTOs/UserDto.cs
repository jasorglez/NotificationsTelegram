namespace NotificationsTelegram.DTOs;

/// <summary>
/// User data from Security microservice
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? IdTelegram { get; set; }
    public bool? AllowWhatsapp { get; set; }
}
