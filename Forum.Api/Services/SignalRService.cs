using Microsoft.AspNetCore.SignalR;
using Forum.Api.Hubs;

namespace Forum.Api.Services;

public class SignalRService : ISignalRService
{
    private readonly IHubContext<TopicsHub> _hubContext;
    private readonly ILogger<SignalRService> _logger;

    public SignalRService(IHubContext<TopicsHub> hubContext, ILogger<SignalRService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyPostCreatedAsync(long topicId, object postData)
    {
        var groupName = $"topic:{topicId}";
        await _hubContext.Clients.Group(groupName).SendAsync("PostCreated", postData);
        _logger.LogDebug("Notified PostCreated for topic {TopicId}", topicId);
    }

    public async Task NotifyPostEditedAsync(long topicId, object postData)
    {
        var groupName = $"topic:{topicId}";
        await _hubContext.Clients.Group(groupName).SendAsync("PostEdited", postData);
        _logger.LogDebug("Notified PostEdited for topic {TopicId}", topicId);
    }

    public async Task NotifyPostDeletedAsync(long topicId, long postId)
    {
        var groupName = $"topic:{topicId}";
        await _hubContext.Clients.Group(groupName).SendAsync("PostDeleted", new { TopicId = topicId, PostId = postId });
        _logger.LogDebug("Notified PostDeleted for topic {TopicId}, post {PostId}", topicId, postId);
    }

    public async Task NotifyTopicStatsUpdatedAsync(long topicId, object stats)
    {
        var groupName = $"topic:{topicId}";
        await _hubContext.Clients.Group(groupName).SendAsync("TopicStats", stats);
        _logger.LogDebug("Notified TopicStats for topic {TopicId}", topicId);
    }
}