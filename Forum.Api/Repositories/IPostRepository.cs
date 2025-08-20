using Forum.Api.Models.Entities;

namespace Forum.Api.Repositories;

public interface IPostRepository
{
    Task<IEnumerable<Post>> GetByTopicIdAsync(long topicId, int limit = 20, long? cursorId = null, DateTime? cursorCreated = null);
    Task<Post?> GetByIdAsync(long id);
    Task<long> CreateAsync(Post post);
    Task UpdateAsync(Post post);
    Task DeleteAsync(long id);
}