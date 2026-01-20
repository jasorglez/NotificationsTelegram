using Microsoft.AspNetCore.SignalR;

namespace NotificationsTelegram.Hub;

public class NotificationHub : Microsoft.AspNetCore.SignalR.Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a user-specific group for targeted notifications
    /// </summary>
    public async Task JoinUserGroup(int userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        _logger.LogInformation("Connection {ConnectionId} joined group user_{UserId}",
            Context.ConnectionId, userId);
    }

    /// <summary>
    /// Leave a user-specific group
    /// </summary>
    public async Task LeaveUserGroup(int userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        _logger.LogInformation("Connection {ConnectionId} left group user_{UserId}",
            Context.ConnectionId, userId);
    }
}
