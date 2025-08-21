namespace Forum.Api.Services;

public interface ISignalRService
{
    Task NotifyPostCreatedAsync(long topicId, object postData);
    Task NotifyPostEditedAsync(long topicId, object postData);
    Task NotifyPostDeletedAsync(long topicId, long postId);
    Task NotifyTopicStatsUpdatedAsync(long topicId, object stats);
}