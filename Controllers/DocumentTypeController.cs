using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotificationsTelegram.Models;

namespace NotificationsTelegram.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DocumentTypeController : ControllerBase
{
    private readonly NotificationsDbContext _context;
    private readonly ILogger<DocumentTypeController> _logger;

    public DocumentTypeController(
        NotificationsDbContext context,
        ILogger<DocumentTypeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all document types
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<DocumentType>>> GetAll()
    {
        var documentTypes = await _context.DocumentTypes
            .Where(d => d.Active)
            .OrderBy(d => d.Code)
            .ToListAsync();

        return Ok(documentTypes);
    }

    /// <summary>
    /// Get document type by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<DocumentType>> GetById(int id)
    {
        var documentType = await _context.DocumentTypes.FindAsync(id);

        if (documentType == null)
        {
            return NotFound(new { message = $"Document type {id} not found" });
        }

        return Ok(documentType);
    }

    /// <summary>
    /// Get document type by code
    /// </summary>
    [HttpGet("code/{code}")]
    public async Task<ActionResult<DocumentType>> GetByCode(string code)
    {
        var documentType = await _context.DocumentTypes
            .FirstOrDefaultAsync(d => d.Code == code && d.Active);

        if (documentType == null)
        {
            return NotFound(new { message = $"Document type '{code}' not found" });
        }

        return Ok(documentType);
    }

    /// <summary>
    /// Create a new document type
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<DocumentType>> Create([FromBody] DocumentType documentType)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if code already exists
        var existing = await _context.DocumentTypes
            .FirstOrDefaultAsync(d => d.Code == documentType.Code);

        if (existing != null)
        {
            return Conflict(new { message = $"Document type with code '{documentType.Code}' already exists" });
        }

        documentType.CreatedAt = DateTime.Now;
        documentType.Active = true;

        _context.DocumentTypes.Add(documentType);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created document type: {Code} - {Description}",
            documentType.Code, documentType.Description);

        return CreatedAtAction(nameof(GetById), new { id = documentType.Id }, documentType);
    }

    /// <summary>
    /// Update a document type
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<DocumentType>> Update(int id, [FromBody] DocumentType documentType)
    {
        if (id != documentType.Id)
        {
            return BadRequest(new { message = "ID mismatch" });
        }

        var existing = await _context.DocumentTypes.FindAsync(id);
        if (existing == null)
        {
            return NotFound(new { message = $"Document type {id} not found" });
        }

        existing.Code = documentType.Code;
        existing.Description = documentType.Description;
        existing.Microservice = documentType.Microservice;
        existing.BaseUrl = documentType.BaseUrl;
        existing.CallbackEndpoint = documentType.CallbackEndpoint;
        existing.ViewUrl = documentType.ViewUrl;
        existing.Active = documentType.Active;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated document type: {Id} - {Code}", id, documentType.Code);

        return Ok(existing);
    }

    /// <summary>
    /// Delete (deactivate) a document type
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var documentType = await _context.DocumentTypes.FindAsync(id);

        if (documentType == null)
        {
            return NotFound(new { message = $"Document type {id} not found" });
        }

        documentType.Active = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deactivated document type: {Id} - {Code}", id, documentType.Code);

        return NoContent();
    }
}
