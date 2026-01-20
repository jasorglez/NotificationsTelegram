using NotificationsTelegram.DTOs;
using NotificationsTelegram.Models;
using System.Text;
using System.Text.Json;

namespace NotificationsTelegram.Services;

public class CallbackService : ICallbackService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CallbackService> _logger;

    public CallbackService(
        HttpClient httpClient,
        ILogger<CallbackService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> SendCallbackAsync(Notification notification, string? authorizeName)
    {
        try
        {
            if (notification.DocumentType == null)
            {
                _logger.LogError("DocumentType is null for notification {NotificationId}", notification.Id);
                return false;
            }

            // Build callback URL
            var callbackUrl = notification.DocumentType.BaseUrl +
                notification.DocumentType.CallbackEndpoint.Replace("{id}", notification.DocumentId.ToString());

            // Build callback data
            var callbackData = new AuthorizationCallbackDto
            {
                DocumentId = notification.DocumentId,
                Folio = notification.Folio,
                Status = notification.Status,
                IdAuthorize = notification.IdAuthorize,
                AuthorizeName = authorizeName,
                RejectionReason = notification.RejectionReason,
                RespondedAt = notification.RespondedAt ?? DateTime.Now
            };

            var json = JsonSerializer.Serialize(callbackData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending callback to {Url} with data: {Data}", callbackUrl, json);

            // Send PUT request to update the document
            var response = await _httpClient.PutAsync(callbackUrl, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Callback sent successfully to {Url} for notification {NotificationId}",
                    callbackUrl, notification.Id);
                return true;
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Callback failed. Status: {Status}, Response: {Response}",
                    response.StatusCode, responseContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending callback for notification {NotificationId}", notification.Id);
            return false;
        }
    }
}
