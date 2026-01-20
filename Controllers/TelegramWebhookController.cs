using Microsoft.AspNetCore.Mvc;
using NotificationsTelegram.Services;
using Telegram.Bot.Types;

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
    public async Task<IActionResult> Webhook([FromBody] Update update)
    {
        _logger.LogInformation("Received Telegram update: {UpdateId}, Type: {Type}",
            update.Id,
            update.Type);

        try
        {
            await _telegramService.ProcessUpdateAsync(update);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Telegram update {UpdateId}", update.Id);
            // Return OK to Telegram to avoid retries
            return Ok();
        }
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
