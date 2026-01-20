using Microsoft.EntityFrameworkCore;
using NotificationsTelegram.DTOs;

namespace NotificationsTelegram.Services;

public class SecurityService : ISecurityService
{
    private readonly SecurityDbContext _context;
    private readonly ILogger<SecurityService> _logger;

    public SecurityService(
        SecurityDbContext context,
        ILogger<SecurityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        try
        {
            var user = await _context.Users
                .Where(u => u.Id == userId && u.Active == 1)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found in Security database", userId);
                return null;
            }

            return new UserDto
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email,
                Phone = user.Phone,
                IdTelegram = user.IdTelegram,
                AllowWhatsapp = user.AllowWhatsapp
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId} from Security database", userId);
            return null;
        }
    }

    public async Task<string?> GetUserTelegramIdAsync(int userId)
    {
        var user = await GetUserByIdAsync(userId);
        return user?.IdTelegram;
    }
}
