using NotificationsTelegram.Models;

namespace NotificationsTelegram.Services;

public interface ICallbackService
{
    /// <summary>
    /// Send callback to origin microservice when document is approved/rejected
    /// </summary>
    Task<bool> SendCallbackAsync(Notification notification, string? authorizeName);
}
