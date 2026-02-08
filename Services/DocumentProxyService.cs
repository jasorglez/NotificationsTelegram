using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace NotificationsTelegram.Services;

public class DocumentProxyService : IDocumentProxyService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DocumentProxyService> _logger;
    private readonly IConfiguration _configuration;

    public DocumentProxyService(
        HttpClient httpClient,
        ILogger<DocumentProxyService> logger,
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

    public async Task<object?> GetDocumentDataAsync(string microservice, string baseUrl, string documentCode, int documentId)
    {
        try
        {
            var token = GenerateJwtToken();
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Determine the endpoint based on document type
            string documentEndpoint;
            string? detailsEndpoint = null;

            switch (documentCode.ToUpper())
            {
                case "OC":
                case "REQUIS":
                    documentEndpoint = $"{baseUrl}/api/Ocandreq/{documentId}";
                    detailsEndpoint = $"{baseUrl}/api/Detailsreqoc/movement/{documentId}";
                    break;
                case "INCOME":
                case "EXPENSE":
                    documentEndpoint = $"{baseUrl}/api/Incomeandexpense/{documentId}";
                    break;
                default:
                    _logger.LogWarning("Unknown document code: {DocumentCode}", documentCode);
                    return null;
            }

            _logger.LogInformation("Fetching document from {Endpoint}", documentEndpoint);

            // Get main document
            var docResponse = await _httpClient.GetAsync(documentEndpoint);
            if (!docResponse.IsSuccessStatusCode)
            {
                var errorContent = await docResponse.Content.ReadAsStringAsync();
                _logger.LogError("Failed to fetch document. Status: {Status}, Response: {Response}",
                    docResponse.StatusCode, errorContent);
                return null;
            }

            var docJson = await docResponse.Content.ReadAsStringAsync();
            var document = JsonSerializer.Deserialize<JsonElement>(docJson);

            // Get details if applicable
            JsonElement? details = null;
            if (!string.IsNullOrEmpty(detailsEndpoint))
            {
                var detailsResponse = await _httpClient.GetAsync(detailsEndpoint);
                if (detailsResponse.IsSuccessStatusCode)
                {
                    var detailsJson = await detailsResponse.Content.ReadAsStringAsync();
                    details = JsonSerializer.Deserialize<JsonElement>(detailsJson);
                }
                else
                {
                    _logger.LogWarning("Failed to fetch details from {Endpoint}", detailsEndpoint);
                }
            }

            return new
            {
                document,
                details
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching document data for {DocumentCode} {DocumentId}", documentCode, documentId);
            return null;
        }
    }
}
