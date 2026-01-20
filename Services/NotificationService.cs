using Microsoft.EntityFrameworkCore;
using NotificationsTelegram.DTOs;
using NotificationsTelegram.Models;

namespace NotificationsTelegram.Services;

public class NotificationService : INotificationService
{
    private readonly NotificationsDbContext _context;
    private readonly ITelegramService _telegramService;
    private readonly ISecurityService _securityService;
    private readonly ICallbackService _callbackService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        NotificationsDbContext context,
        ITelegramService telegramService,
        ISecurityService securityService,
        ICallbackService callbackService,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _telegramService = telegramService;
        _securityService = securityService;
        _callbackService = callbackService;
        _logger = logger;
    }

    public async Task<NotificationResponse> SendNotificationAsync(SendNotificationRequest request)
    {
        try
        {
            // Get document type
            var documentType = await _context.DocumentTypes
                .FirstOrDefaultAsync(d => d.Code == request.DocumentType && d.Active);

            if (documentType == null)
            {
                return new NotificationResponse
                {
                    Success = false,
                    Error = $"Document type '{request.DocumentType}' not found or inactive"
                };
            }

            // Get authorizer's Telegram ID
            var authorizer = await _securityService.GetUserByIdAsync(request.IdAuthorize);
            if (authorizer == null || string.IsNullOrEmpty(authorizer.IdTelegram))
            {
                return new NotificationResponse
                {
                    Success = false,
                    Error = $"Authorizer (ID: {request.IdAuthorize}) not found or has no Telegram ID configured"
                };
            }

            // Get solicit user name
            var solicit = await _securityService.GetUserByIdAsync(request.IdSolicit);
            var solicitName = solicit?.DisplayName ?? $"Usuario {request.IdSolicit}";

            // Create notification record
            var notification = new Notification
            {
                IdDocumentType = documentType.Id,
                DocumentId = request.DocumentId,
                Folio = request.Folio,
                Description = request.Description,
                IdSolicit = request.IdSolicit,
                IdAuthorize = request.IdAuthorize,
                TelegramChatId = long.TryParse(authorizer.IdTelegram, out var chatId) ? chatId : null,
                Status = NotificationStatus.PENDING,
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Add log
            await AddLogAsync(notification.Id, LogAction.CREATED, $"Notification created for {request.DocumentType} {request.Folio}");

            // Build view URL
            var viewUrl = documentType.ViewUrl?
                .Replace("{folio}", request.Folio)
                .Replace("{id}", request.DocumentId.ToString())
                ?? $"{documentType.BaseUrl}/view/{request.DocumentId}";

            // Send Telegram message
            notification.DocumentType = documentType; // Set for message building
            var message = await _telegramService.SendAuthorizationRequestAsync(
                notification,
                authorizer.IdTelegram,
                solicitName,
                viewUrl
            );

            if (message != null)
            {
                notification.TelegramMsgId = message.MessageId;
                notification.SentAt = DateTime.Now;
                await _context.SaveChangesAsync();

                await AddLogAsync(notification.Id, LogAction.SENT, $"Message sent to Telegram. MessageId: {message.MessageId}");

                return new NotificationResponse
                {
                    Success = true,
                    NotificationId = notification.Id,
                    Message = "Notification sent successfully"
                };
            }
            else
            {
                notification.Status = NotificationStatus.ERROR;
                await _context.SaveChangesAsync();

                await AddLogAsync(notification.Id, LogAction.ERROR, "Failed to send Telegram message");

                return new NotificationResponse
                {
                    Success = false,
                    NotificationId = notification.Id,
                    Error = "Failed to send Telegram message"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification");
            return new NotificationResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<Notification?> GetByIdAsync(int id)
    {
        return await _context.Notifications
            .Include(n => n.DocumentType)
            .Include(n => n.Logs)
            .FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<NotificationStatusResponse?> GetStatusAsync(int id)
    {
        var notification = await _context.Notifications
            .Include(n => n.DocumentType)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (notification == null) return null;

        var solicit = await _securityService.GetUserByIdAsync(notification.IdSolicit);
        var authorizer = await _securityService.GetUserByIdAsync(notification.IdAuthorize);

        return new NotificationStatusResponse
        {
            Id = notification.Id,
            DocumentType = notification.DocumentType?.Code ?? "",
            Folio = notification.Folio,
            Status = notification.Status,
            RejectionReason = notification.RejectionReason,
            CreatedAt = notification.CreatedAt,
            RespondedAt = notification.RespondedAt,
            SolicitName = solicit?.DisplayName,
            AuthorizeName = authorizer?.DisplayName
        };
    }

    public async Task<List<PendingNotificationResponse>> GetPendingByAuthorizerAsync(int userId)
    {
        var notifications = await _context.Notifications
            .Include(n => n.DocumentType)
            .Where(n => n.IdAuthorize == userId && n.Status == NotificationStatus.PENDING && n.Active)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        var result = new List<PendingNotificationResponse>();

        foreach (var n in notifications)
        {
            var solicit = await _securityService.GetUserByIdAsync(n.IdSolicit);

            var viewUrl = n.DocumentType?.ViewUrl?
                .Replace("{folio}", n.Folio)
                .Replace("{id}", n.DocumentId.ToString());

            result.Add(new PendingNotificationResponse
            {
                Id = n.Id,
                DocumentType = n.DocumentType?.Code ?? "",
                DocumentTypeDescription = n.DocumentType?.Description ?? "",
                Folio = n.Folio,
                Description = n.Description,
                SolicitName = solicit?.DisplayName,
                CreatedAt = n.CreatedAt,
                ViewUrl = viewUrl
            });
        }

        return result;
    }

    public async Task<List<NotificationStatusResponse>> GetHistoryAsync(
        int? idSolicit = null,
        int? idAuthorize = null,
        string? status = null)
    {
        var query = _context.Notifications
            .Include(n => n.DocumentType)
            .Where(n => n.Active);

        if (idSolicit.HasValue)
            query = query.Where(n => n.IdSolicit == idSolicit.Value);

        if (idAuthorize.HasValue)
            query = query.Where(n => n.IdAuthorize == idAuthorize.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(n => n.Status == status);

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .ToListAsync();

        var result = new List<NotificationStatusResponse>();

        foreach (var n in notifications)
        {
            var solicit = await _securityService.GetUserByIdAsync(n.IdSolicit);
            var authorizer = await _securityService.GetUserByIdAsync(n.IdAuthorize);

            result.Add(new NotificationStatusResponse
            {
                Id = n.Id,
                DocumentType = n.DocumentType?.Code ?? "",
                Folio = n.Folio,
                Status = n.Status,
                RejectionReason = n.RejectionReason,
                CreatedAt = n.CreatedAt,
                RespondedAt = n.RespondedAt,
                SolicitName = solicit?.DisplayName,
                AuthorizeName = authorizer?.DisplayName
            });
        }

        return result;
    }

    public async Task<bool> ApproveAsync(int notificationId)
    {
        var notification = await GetByIdAsync(notificationId);
        if (notification == null) return false;

        notification.Status = NotificationStatus.APPROVED;
        notification.RespondedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        await AddLogAsync(notificationId, LogAction.APPROVED, "Document approved");

        // Edit original message
        if (notification.TelegramChatId.HasValue && notification.TelegramMsgId.HasValue)
        {
            await _telegramService.EditMessageAsProcessedAsync(
                notification.TelegramChatId.Value.ToString(),
                notification.TelegramMsgId.Value,
                NotificationStatus.APPROVED
            );
        }

        // Send callback to origin microservice
        var authorizer = await _securityService.GetUserByIdAsync(notification.IdAuthorize);
        var callbackSent = await _callbackService.SendCallbackAsync(notification, authorizer?.DisplayName);

        notification.CallbackSent = callbackSent;
        await _context.SaveChangesAsync();

        if (callbackSent)
        {
            await AddLogAsync(notificationId, LogAction.CALLBACK_SENT, "Callback sent to origin microservice");
        }
        else
        {
            await AddLogAsync(notificationId, LogAction.CALLBACK_FAILED, "Failed to send callback");
        }

        // Notify solicit user
        await NotifySolicitAsync(notification, authorizer?.DisplayName);

        return true;
    }

    public async Task<bool> SetAwaitingReasonAsync(int notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification == null) return false;

        notification.Status = NotificationStatus.AWAITING_REASON;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RejectWithReasonAsync(int notificationId, string reason)
    {
        var notification = await GetByIdAsync(notificationId);
        if (notification == null) return false;

        notification.Status = NotificationStatus.REJECTED;
        notification.RejectionReason = reason;
        notification.RespondedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        await AddLogAsync(notificationId, LogAction.REJECTED, $"Document rejected. Reason: {reason}");

        // Edit original message
        if (notification.TelegramChatId.HasValue && notification.TelegramMsgId.HasValue)
        {
            await _telegramService.EditMessageAsProcessedAsync(
                notification.TelegramChatId.Value.ToString(),
                notification.TelegramMsgId.Value,
                NotificationStatus.REJECTED,
                reason
            );
        }

        // Send callback to origin microservice
        var authorizer = await _securityService.GetUserByIdAsync(notification.IdAuthorize);
        var callbackSent = await _callbackService.SendCallbackAsync(notification, authorizer?.DisplayName);

        notification.CallbackSent = callbackSent;
        await _context.SaveChangesAsync();

        if (callbackSent)
        {
            await AddLogAsync(notificationId, LogAction.CALLBACK_SENT, "Callback sent to origin microservice");
        }
        else
        {
            await AddLogAsync(notificationId, LogAction.CALLBACK_FAILED, "Failed to send callback");
        }

        // Notify solicit user
        await NotifySolicitAsync(notification, authorizer?.DisplayName);

        return true;
    }

    public async Task<Notification?> GetAwaitingReasonByChatIdAsync(string chatId)
    {
        if (!long.TryParse(chatId, out var telegramChatId)) return null;

        return await _context.Notifications
            .Include(n => n.DocumentType)
            .FirstOrDefaultAsync(n =>
                n.TelegramChatId == telegramChatId &&
                n.Status == NotificationStatus.AWAITING_REASON &&
                n.Active);
    }

    public async Task AddLogAsync(int notificationId, string action, string? detail = null)
    {
        var log = new NotificationLog
        {
            IdNotification = notificationId,
            Action = action,
            Detail = detail,
            CreatedAt = DateTime.Now
        };

        _context.NotificationLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    private async Task NotifySolicitAsync(Notification notification, string? authorizeName)
    {
        try
        {
            var solicit = await _securityService.GetUserByIdAsync(notification.IdSolicit);
            if (solicit == null || string.IsNullOrEmpty(solicit.IdTelegram))
            {
                _logger.LogWarning("Cannot notify solicit user {UserId}: no Telegram ID", notification.IdSolicit);
                return;
            }

            var message = await _telegramService.SendResultToSolicitAsync(
                notification,
                solicit.IdTelegram,
                authorizeName ?? "Usuario"
            );

            if (message != null)
            {
                notification.SolicitNotified = true;
                await _context.SaveChangesAsync();
                await AddLogAsync(notification.Id, LogAction.SOLICIT_NOTIFIED, $"Solicit user notified via Telegram");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying solicit user for notification {NotificationId}", notification.Id);
        }
    }
}
