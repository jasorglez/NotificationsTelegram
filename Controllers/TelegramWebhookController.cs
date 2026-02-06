using Microsoft.AspNetCore.Mvc;
using NotificationsTelegram.Services;
using Telegram.Bot.Types;
using System.Text.Json;

namespace NotificationsTelegram.Controllers;

[ApiController]
[Route("api/telegram")]
public class TelegramWebhookController : ControllerBase
{
    private readonly ITelegramService _telegramService;
    private readonly ILogger<TelegramWebhookController> _logger;

    public TelegramWebhookController(
        ITelegramService telegramService,
        ILogger<TelegramWebhookController> logger)
    {
        _telegramService = telegramService;
        _logger = logger;
    }

    /// <summary>
    /// Telegram webhook endpoint to receive updates
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync();

        _logger.LogInformation("Received webhook payload: {Length} chars", json.Length);

        try
        {
            var update = JsonSerializer.Deserialize<Update>(json);

            if (update == null)
            {
                _logger.LogWarning("Failed to deserialize update, payload: {Json}", json);
                return Ok();
            }

            _logger.LogInformation("Parsed Telegram update: {UpdateId}, Type: {Type}",
                update.Id, update.Type);

            await _telegramService.ProcessUpdateAsync(update);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Telegram webhook");
        }

        // Always return OK to Telegram to avoid retries
        return Ok();
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.Now });
    }
}
