using NotificationsTelegram.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace NotificationsTelegram.Services;

public class TelegramService : ITelegramService
{
    private readonly TelegramBotClient _botClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public TelegramService(
        IConfiguration configuration,
        ILogger<TelegramService> logger,
        IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _logger = logger;
        _serviceProvider = serviceProvider;

        var botToken = _configuration["Telegram:BotToken"]
            ?? throw new ArgumentNullException("Telegram:BotToken not configured");
        _botClient = new TelegramBotClient(botToken);
    }

    public async Task<Message?> SendAuthorizationRequestAsync(
        Notification notification,
        string chatId,
        string solicitName,
        string viewUrl)
    {
        try
        {
            var docType = notification.DocumentType?.Description ?? notification.DocumentType?.Code ?? "Documento";

            var messageText = $"📋 *Solicitud de Autorización*\n\n" +
                              $"*Tipo:* {docType}\n" +
                              $"*Folio:* `{notification.Folio}`\n" +
                              $"*Solicitante:* {solicitName}\n";

            if (!string.IsNullOrEmpty(notification.Description))
            {
                messageText += $"*Descripción:* {notification.Description}\n";
            }

            messageText += $"\n_Fecha: {notification.CreatedAt:dd/MM/yyyy HH:mm}_";

            // Inline keyboard with buttons
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Autorizar", $"approve_{notification.Id}"),
                    InlineKeyboardButton.WithCallbackData("❌ Rechazar", $"reject_{notification.Id}")
                },
                new[]
                {
                    InlineKeyboardButton.WithUrl("👁️ Ver detalle", viewUrl)
                }
            });

            var message = await _botClient.SendMessage(
                chatId: chatId,
                text: messageText,
                parseMode: ParseMode.Markdown,
                replyMarkup: inlineKeyboard
            );

            _logger.LogInformation("Authorization request sent to {ChatId} for notification {NotificationId}",
                chatId, notification.Id);

            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending authorization request to {ChatId}", chatId);
            return null;
        }
    }

    public async Task<Message?> SendResultToSolicitAsync(
        Notification notification,
        string chatId,
        string authorizeName)
    {
        try
        {
            var docType = notification.DocumentType?.Description ?? "Documento";
            var emoji = notification.Status == NotificationStatus.APPROVED ? "✅" : "❌";
            var statusText = notification.Status == NotificationStatus.APPROVED ? "Autorizado" : "Rechazado";

            var messageText = $"{emoji} *Documento {statusText}*\n\n" +
                              $"Tu *{docType}* con folio `{notification.Folio}` fue {statusText.ToLower()} por *{authorizeName}*.\n";

            if (notification.Status == NotificationStatus.REJECTED && !string.IsNullOrEmpty(notification.RejectionReason))
            {
                messageText += $"\n*Motivo:* {notification.RejectionReason}\n";
            }

            messageText += $"\n_Fecha: {notification.RespondedAt:dd/MM/yyyy HH:mm}_";

            // Optional: Add view button
            InlineKeyboardMarkup? replyMarkup = null;
            if (!string.IsNullOrEmpty(notification.DocumentType?.ViewUrl))
            {
                var viewUrl = notification.DocumentType.ViewUrl
                    .Replace("{folio}", notification.Folio)
                    .Replace("{id}", notification.DocumentId.ToString());

                replyMarkup = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithUrl("👁️ Ver documento", viewUrl)
                });
            }

            var message = await _botClient.SendMessage(
                chatId: chatId,
                text: messageText,
                parseMode: ParseMode.Markdown,
                replyMarkup: replyMarkup
            );

            _logger.LogInformation("Result notification sent to solicit {ChatId} for notification {NotificationId}",
                chatId, notification.Id);

            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending result to solicit {ChatId}", chatId);
            return null;
        }
    }

    public async Task<Message?> SendAskReasonAsync(string chatId, int notificationId)
    {
        try
        {
            var messageText = "📝 Por favor escribe el *motivo del rechazo*:";

            var message = await _botClient.SendMessage(
                chatId: chatId,
                text: messageText,
                parseMode: ParseMode.Markdown,
                replyMarkup: new ForceReplyMarkup { Selective = true }
            );

            _logger.LogInformation("Asked for rejection reason to {ChatId} for notification {NotificationId}",
                chatId, notificationId);

            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error asking for reason to {ChatId}", chatId);
            return null;
        }
    }

    public async Task EditMessageAsProcessedAsync(string chatId, long messageId, string status, string? reason = null)
    {
        try
        {
            var emoji = status == NotificationStatus.APPROVED ? "✅" : "❌";
            var statusText = status == NotificationStatus.APPROVED ? "AUTORIZADO" : "RECHAZADO";

            var newText = $"{emoji} *{statusText}*";
            if (!string.IsNullOrEmpty(reason))
            {
                newText += $"\n\n*Motivo:* {reason}";
            }

            await _botClient.EditMessageText(
                chatId: chatId,
                messageId: (int)messageId,
                text: newText,
                parseMode: ParseMode.Markdown,
                replyMarkup: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing message {MessageId} in chat {ChatId}", messageId, chatId);
        }
    }

    public async Task ProcessUpdateAsync(Update update)
    {
        try
        {
            // Handle callback query (button press)
            if (update.CallbackQuery != null)
            {
                await ProcessCallbackQueryAsync(update.CallbackQuery);
                return;
            }

            // Handle text message (rejection reason)
            if (update.Message?.Text != null)
            {
                await ProcessTextMessageAsync(update.Message);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing update {UpdateId}", update.Id);
        }
    }

    private async Task ProcessCallbackQueryAsync(CallbackQuery callbackQuery)
    {
        var data = callbackQuery.Data;
        if (string.IsNullOrEmpty(data)) return;

        var chatId = callbackQuery.Message?.Chat.Id.ToString();
        if (chatId == null) return;

        _logger.LogInformation("Processing callback: {Data} from chat {ChatId}", data, chatId);

        // Parse callback data
        var parts = data.Split('_');
        if (parts.Length != 2) return;

        var action = parts[0];
        if (!int.TryParse(parts[1], out var notificationId)) return;

        // Get services from scope
        using var scope = _serviceProvider.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        if (action == "approve")
        {
            await notificationService.ApproveAsync(notificationId);

            // Answer callback
            await _botClient.AnswerCallbackQuery(callbackQuery.Id, "✅ Documento autorizado");
        }
        else if (action == "reject")
        {
            // Change status to awaiting reason
            await notificationService.SetAwaitingReasonAsync(notificationId);

            // Ask for reason
            await SendAskReasonAsync(chatId, notificationId);

            // Answer callback
            await _botClient.AnswerCallbackQuery(callbackQuery.Id, "📝 Escribe el motivo del rechazo");
        }
    }

    private async Task ProcessTextMessageAsync(Message message)
    {
        var chatId = message.Chat.Id.ToString();
        var text = message.Text;

        if (string.IsNullOrEmpty(text)) return;

        _logger.LogInformation("Processing text message from chat {ChatId}: {Text}", chatId, text);

        // Check if there's a notification awaiting reason for this chat
        using var scope = _serviceProvider.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var awaitingNotification = await notificationService.GetAwaitingReasonByChatIdAsync(chatId);

        if (awaitingNotification != null)
        {
            // This is the rejection reason
            await notificationService.RejectWithReasonAsync(awaitingNotification.Id, text);

            // Confirm to user
            await _botClient.SendMessage(
                chatId: chatId,
                text: "✅ Documento rechazado. Se ha notificado al solicitante.",
                parseMode: ParseMode.Markdown
            );
        }
    }
}
