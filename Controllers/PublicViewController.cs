using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotificationsTelegram.Services;

namespace NotificationsTelegram.Controllers;

[ApiController]
[Route("api/public")]
public class PublicViewController : ControllerBase
{
    private readonly NotificationsDbContext _context;
    private readonly IDocumentProxyService _documentProxyService;
    private readonly ISecurityService _securityService;
    private readonly ILogger<PublicViewController> _logger;

    private const int TOKEN_EXPIRY_HOURS = 72;

    public PublicViewController(
        NotificationsDbContext context,
        IDocumentProxyService documentProxyService,
        ISecurityService securityService,
        ILogger<PublicViewController> logger)
    {
        _context = context;
        _documentProxyService = documentProxyService;
        _securityService = securityService;
        _logger = logger;
    }

    /// <summary>
    /// Public endpoint - validates access token and returns document data (no JWT required)
    /// </summary>
    [HttpGet("view/{token}")]
    public async Task<IActionResult> GetDocumentByToken(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { error = "Token is required" });
            }

            // Find notification by access token
            var notification = await _context.Notifications
                .Include(n => n.DocumentType)
                .FirstOrDefaultAsync(n => n.AccessToken == token && n.Active);

            if (notification == null)
            {
                return NotFound(new { error = "Enlace inválido o no encontrado" });
            }

            // Check token expiry (72 hours)
            if (DateTime.Now.Subtract(notification.CreatedAt).TotalHours > TOKEN_EXPIRY_HOURS)
            {
                return BadRequest(new { error = "Este enlace ha expirado. Los enlaces son válidos por 72 horas." });
            }

            if (notification.DocumentType == null)
            {
                return BadRequest(new { error = "Tipo de documento no encontrado" });
            }

            // Get solicit and authorizer names
            var solicit = await _securityService.GetUserByIdAsync(notification.IdSolicit);
            var authorizer = await _securityService.GetUserByIdAsync(notification.IdAuthorize);

            // Get document data from origin microservice
            var documentData = await _documentProxyService.GetDocumentDataAsync(
                notification.DocumentType.Microservice,
                notification.DocumentType.BaseUrl,
                notification.DocumentType.Code,
                notification.DocumentId
            );

            return Ok(new
            {
                notification = new
                {
                    id = notification.Id,
                    folio = notification.Folio,
                    description = notification.Description,
                    status = notification.Status,
                    rejectionReason = notification.RejectionReason,
                    createdAt = notification.CreatedAt,
                    respondedAt = notification.RespondedAt,
                    solicitName = solicit?.DisplayName,
                    authorizeName = authorizer?.DisplayName
                },
                documentType = new
                {
                    code = notification.DocumentType.Code,
                    description = notification.DocumentType.Description
                },
                documentData
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document by token {Token}", token);
            return StatusCode(500, new { error = "Error interno del servidor" });
        }
    }
}
