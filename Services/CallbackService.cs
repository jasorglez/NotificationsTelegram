using Microsoft.IdentityModel.Tokens;
using NotificationsTelegram.DTOs;
using NotificationsTelegram.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace NotificationsTelegram.Services;

public class CallbackService : ICallbackService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CallbackService> _logger;
    private readonly IConfiguration _configuration;

    public CallbackService(
        HttpClient httpClient,
        ILogger<CallbackService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    private string GenerateJwtToken()
    {
        var key = _configuration["Jwt:Key"];
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "NotificationsTelegram"),
            new Claim(ClaimTypes.Role, "Service")
        };

        var token = new JwtSecurityToken(
            issuer: null,
            audience: null,
            claims: claims,
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
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

            // Generate JWT token for authentication
            var token = GenerateJwtToken();
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            _logger.LogInformation("Sending PATCH callback to {Url} with data: {Data}", callbackUrl, json);

            // Send PATCH request to update the document authorization
            var request = new HttpRequestMessage(HttpMethod.Patch, callbackUrl)
            {
                Content = content
            };
            var response = await _httpClient.SendAsync(request);

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
