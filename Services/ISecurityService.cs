using NotificationsTelegram.DTOs;

namespace NotificationsTelegram.Services;

public interface ISecurityService
{
    /// <summary>
    /// Get user data from Security microservice
    /// </summary>
    Task<UserDto?> GetUserByIdAsync(int userId);

    /// <summary>
    /// Get user Telegram ID
    /// </summary>
    Task<string?> GetUserTelegramIdAsync(int userId);
}
