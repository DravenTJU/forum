using Forum.Api.Models.Entities;

namespace Forum.Api.Services;

public interface IPostService
{
    Task<IEnumerable<Post>> GetByTopicIdAsync(long topicId, int limit = 20, long? cursorId = null, DateTime? cursorCreated = null);
    Task<Post?> GetByIdAsync(long id);
    Task<long> CreateAsync(long topicId, long authorId, string contentMd, long? replyToPostId = null);
    Task UpdateAsync(long id, string contentMd);
    Task DeleteAsync(long id);
}