using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Forum.Api.Hubs;

[Authorize]
public class TopicsHub : Hub
{
    private readonly ILogger<TopicsHub> _logger;

    public TopicsHub(ILogger<TopicsHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinTopic(long topicId)
    {
        var groupName = GetTopicGroupName(topicId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogInformation("User {UserId} joined topic {TopicId}", 
            Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, topicId);
    }

    public async Task LeaveTopic(long topicId)
    {
        var groupName = GetTopicGroupName(topicId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogInformation("User {UserId} left topic {TopicId}", 
            Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, topicId);
    }

    public async Task Typing(long topicId, bool isTyping)
    {
        var groupName = GetTopicGroupName(topicId);
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        await Clients.OthersInGroup(groupName).SendAsync("UserTyping", new
        {
            TopicId = topicId,
            UserId = userId,
            Username = username,
            IsTyping = isTyping,
            Timestamp = DateTime.UtcNow
        });
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("User {UserId} connected to SignalR", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("User {UserId} disconnected from SignalR", userId);
        await base.OnDisconnectedAsync(exception);
    }

    private static string GetTopicGroupName(long topicId) => $"topic:{topicId}";
}