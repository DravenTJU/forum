using Forum.Api.Models.Entities;
using Forum.Api.Repositories;

namespace Forum.Api.Services;

public class PostService : IPostService
{
    private readonly IPostRepository _postRepository;
    private readonly ISignalRService _signalRService;
    private readonly ILogger<PostService> _logger;

    public PostService(
        IPostRepository postRepository, 
        ISignalRService signalRService,
        ILogger<PostService> logger)
    {
        _postRepository = postRepository;
        _signalRService = signalRService;
        _logger = logger;
    }

    public async Task<IEnumerable<Post>> GetByTopicIdAsync(long topicId, int limit = 20, long? cursorId = null, DateTime? cursorCreated = null)
    {
        return await _postRepository.GetByTopicIdAsync(topicId, limit, cursorId, cursorCreated);
    }

    public async Task<Post?> GetByIdAsync(long id)
    {
        return await _postRepository.GetByIdAsync(id);
    }

    public async Task<long> CreateAsync(long topicId, long authorId, string contentMd, long? replyToPostId = null)
    {
        var post = new Post
        {
            TopicId = topicId,
            AuthorId = authorId,
            ContentMd = contentMd,
            ReplyToPostId = replyToPostId,
            IsEdited = false,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var postId = await _postRepository.CreateAsync(post);
        post.Id = postId;

        // 发送实时通知
        await _signalRService.NotifyPostCreatedAsync(topicId, post);

        _logger.LogInformation("Post {PostId} created in topic {TopicId} by user {UserId}", 
            postId, topicId, authorId);
        
        return postId;
    }

    public async Task UpdateAsync(long id, string contentMd)
    {
        var post = await _postRepository.GetByIdAsync(id);
        if (post == null)
        {
            throw new KeyNotFoundException("Post not found");
        }

        post.ContentMd = contentMd;
        post.IsEdited = true;
        post.UpdatedAt = DateTime.UtcNow;

        await _postRepository.UpdateAsync(post);

        // 发送实时通知
        await _signalRService.NotifyPostEditedAsync(post.TopicId, post);

        _logger.LogInformation("Post {PostId} updated", id);
    }

    public async Task DeleteAsync(long id)
    {
        var post = await _postRepository.GetByIdAsync(id);
        if (post == null)
        {
            throw new KeyNotFoundException("Post not found");
        }

        await _postRepository.DeleteAsync(id);

        // 发送实时通知
        await _signalRService.NotifyPostDeletedAsync(post.TopicId, id);

        _logger.LogInformation("Post {PostId} deleted", id);
    }

    public async Task<int> GetReplyCountByTopicIdAsync(long topicId)
    {
        return await _postRepository.GetReplyCountByTopicIdAsync(topicId);
    }

    public async Task<Post?> GetLastPostByTopicIdAsync(long topicId)
    {
        return await _postRepository.GetLastPostByTopicIdAsync(topicId);
    }

    public async Task<Post?> GetFirstPostByTopicIdAsync(long topicId)
    {
        return await _postRepository.GetFirstPostByTopicIdAsync(topicId);
    }

    public async Task<Dictionary<long, int>> GetReplyCountsByTopicIdsAsync(IEnumerable<long> topicIds)
    {
        return await _postRepository.GetReplyCountsByTopicIdsAsync(topicIds);
    }

    public async Task<Dictionary<long, Post?>> GetLastPostsByTopicIdsAsync(IEnumerable<long> topicIds)
    {
        return await _postRepository.GetLastPostsByTopicIdsAsync(topicIds);
    }
}