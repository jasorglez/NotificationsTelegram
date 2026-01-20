using NotificationsTelegram.Models;
using Telegram.Bot.Types;

namespace NotificationsTelegram.Services;

public interface ITelegramService
{
    /// <summary>
    /// Send authorization request message to Telegram
    /// </summary>
    Task<Message?> SendAuthorizationRequestAsync(Notification notification, string chatId, string solicitName, string viewUrl);

    /// <summary>
    /// Send notification to solicit user about the result
    /// </summary>
    Task<Message?> SendResultToSolicitAsync(Notification notification, string chatId, string authorizeName);

    /// <summary>
    /// Send message asking for rejection reason
    /// </summary>
    Task<Message?> SendAskReasonAsync(string chatId, int notificationId);

    /// <summary>
    /// Edit message to show it was processed
    /// </summary>
    Task EditMessageAsProcessedAsync(string chatId, long messageId, string status, string? reason = null);

    /// <summary>
    /// Process incoming update from webhook
    /// </summary>
    Task ProcessUpdateAsync(Update update);
}
