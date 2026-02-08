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

    // Internal microservice URLs
    private const string SMP_BASE_URL = "http://66.179.240.10:5004";
    private const string WAREHOUSE_BASE_URL = "http://66.179.240.10:5007";

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

    private void SetAuthHeader()
    {
        var token = GenerateJwtToken();
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<JsonElement?> FetchJsonAsync(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<JsonElement>(json);
            }
            _logger.LogWarning("Failed to fetch {Url}. Status: {Status}", url, response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching {Url}", url);
        }
        return null;
    }

    private async Task<string?> FetchImageAsBase64Async(string? imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) return null;
        try
        {
            // Use a separate HttpClient without auth headers for public image URLs
            using var imageClient = new HttpClient();
            var response = await imageClient.GetAsync(imageUrl);
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/png";
                return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
            }
            _logger.LogWarning("Failed to fetch image {Url}. Status: {Status}", imageUrl, response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching image {Url}", imageUrl);
        }
        return null;
    }

    public async Task<object?> GetDocumentDataAsync(string microservice, string baseUrl, string documentCode, int documentId)
    {
        try
        {
            SetAuthHeader();

            // Determine endpoints based on document type
            string documentEndpoint;
            string? detailsEndpoint = null;

            switch (documentCode.ToUpper())
            {
                case "OC":
                case "REQUIS":
                    documentEndpoint = $"{baseUrl}/api/Ocandreq/{documentId}";
                    detailsEndpoint = $"{WAREHOUSE_BASE_URL}/api/Detailsreqoc/{documentId}";
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
            var document = await FetchJsonAsync(documentEndpoint);
            if (document == null)
            {
                _logger.LogError("Failed to fetch main document from {Endpoint}", documentEndpoint);
                return null;
            }

            // Get details (items)
            JsonElement? details = null;
            if (!string.IsNullOrEmpty(detailsEndpoint))
            {
                details = await FetchJsonAsync(detailsEndpoint);
            }

            // Get company data (Root) - extract idCompany from document
            JsonElement? companyData = null;
            string? logoBase64 = null;
            string? logo2Base64 = null;
            string? watermarkBase64 = null;

            if (document.Value.TryGetProperty("idCompany", out var idCompanyProp))
            {
                var idCompany = idCompanyProp.GetInt32();
                companyData = await FetchJsonAsync($"{SMP_BASE_URL}/api/Root/{idCompany}");

                // Fetch logos as base64
                if (companyData.HasValue)
                {
                    string? pictureUrl = null, picture2Url = null, picture3Url = null;

                    if (companyData.Value.TryGetProperty("picture", out var picProp) && picProp.ValueKind == JsonValueKind.String)
                        pictureUrl = picProp.GetString();
                    if (companyData.Value.TryGetProperty("picture2", out var pic2Prop) && pic2Prop.ValueKind == JsonValueKind.String)
                        picture2Url = pic2Prop.GetString();
                    if (companyData.Value.TryGetProperty("picture3", out var pic3Prop) && pic3Prop.ValueKind == JsonValueKind.String)
                        picture3Url = pic3Prop.GetString();

                    // Fetch images in parallel
                    var logoTask = FetchImageAsBase64Async(pictureUrl);
                    var logo2Task = FetchImageAsBase64Async(picture2Url);
                    var watermarkTask = FetchImageAsBase64Async(picture3Url);

                    await Task.WhenAll(logoTask, logo2Task, watermarkTask);

                    logoBase64 = logoTask.Result;
                    logo2Base64 = logo2Task.Result ?? logoBase64;
                    watermarkBase64 = watermarkTask.Result;
                }
            }

            // Get provider data - extract idProvider from document
            JsonElement? providerData = null;
            if (document.Value.TryGetProperty("idProvider", out var idProviderProp))
            {
                var idProvider = idProviderProp.GetInt32();
                if (idProvider > 0)
                {
                    providerData = await FetchJsonAsync($"{SMP_BASE_URL}/api/Providers/{idProvider}");
                }
            }

            // Get materials (for mapping descriptions) - only for OC/REQUIS
            JsonElement? materials = null;
            if ((documentCode.ToUpper() == "OC" || documentCode.ToUpper() == "REQUIS") &&
                document.Value.TryGetProperty("idCompany", out var idCompanyForMat))
            {
                var idCompany = idCompanyForMat.GetInt32();
                materials = await FetchJsonAsync($"{WAREHOUSE_BASE_URL}/api/Material/2fields?idCompany={idCompany}");
            }

            return new
            {
                document,
                details,
                companyData,
                providerData,
                materials,
                logoBase64,
                logo2Base64,
                watermarkBase64
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching document data for {DocumentCode} {DocumentId}", documentCode, documentId);
            return null;
        }
    }
}
