using Forum.Api.Models.Entities;

namespace Forum.Api.Services;

public interface ITagService
{
    Task<IEnumerable<Tag>> GetAllAsync();
    Task<Tag?> GetByIdAsync(long id);
    Task<Tag?> GetBySlugAsync(string slug);
    Task<IEnumerable<Tag>> GetByTopicIdAsync(long topicId);
    Task<Dictionary<long, IEnumerable<Tag>>> GetByTopicIdsAsync(IEnumerable<long> topicIds);
}