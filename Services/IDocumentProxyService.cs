namespace NotificationsTelegram.Services;

public interface IDocumentProxyService
{
    Task<object?> GetDocumentDataAsync(string microservice, string baseUrl, string documentCode, int documentId);
}
