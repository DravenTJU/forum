using Forum.Api.Models.Entities;
using Forum.Api.Repositories;

namespace Forum.Api.Services;

public class TopicService : ITopicService
{
    private readonly ITopicRepository _topicRepository;
    private readonly ILogger<TopicService> _logger;

    public TopicService(ITopicRepository topicRepository, ILogger<TopicService> logger)
    {
        _topicRepository = topicRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<Topic>> GetAllAsync(long? categoryId = null, int limit = 20, long? cursorId = null, DateTime? cursorLastPosted = null)
    {
        return await _topicRepository.GetAllAsync(categoryId, limit, cursorId, cursorLastPosted);
    }

    public async Task<Topic?> GetByIdAsync(long id)
    {
        return await _topicRepository.GetByIdAsync(id);
    }

    public async Task<long> CreateAsync(string title, string slug, long authorId, long categoryId)
    {
        var topic = new Topic
        {
            Title = title,
            Slug = slug,
            AuthorId = authorId,
            CategoryId = categoryId,
            IsPinned = false,
            IsLocked = false,
            IsDeleted = false,
            ReplyCount = 0,
            ViewCount = 0,
            LastPostedAt = DateTime.UtcNow,
            LastPosterId = authorId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var topicId = await _topicRepository.CreateAsync(topic);
        _logger.LogInformation("Topic {TopicId} created by user {UserId}", topicId, authorId);
        
        return topicId;
    }

    public async Task UpdateAsync(long id, string title, string slug, long categoryId)
    {
        var topic = await _topicRepository.GetByIdAsync(id);
        if (topic == null)
        {
            throw new KeyNotFoundException("Topic not found");
        }

        topic.Title = title;
        topic.Slug = slug;
        topic.CategoryId = categoryId;
        topic.UpdatedAt = DateTime.UtcNow;

        await _topicRepository.UpdateAsync(topic);
        _logger.LogInformation("Topic {TopicId} updated", id);
    }

    public async Task DeleteAsync(long id)
    {
        var topic = await _topicRepository.GetByIdAsync(id);
        if (topic == null)
        {
            throw new KeyNotFoundException("Topic not found");
        }

        await _topicRepository.DeleteAsync(id);
        _logger.LogInformation("Topic {TopicId} deleted", id);
    }
}