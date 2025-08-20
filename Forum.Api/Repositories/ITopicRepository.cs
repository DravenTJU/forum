using Forum.Api.Models.Entities;

namespace Forum.Api.Repositories;

public interface ITopicRepository
{
    Task<IEnumerable<Topic>> GetAllAsync(long? categoryId = null, int limit = 20, long? cursorId = null, DateTime? cursorLastPosted = null);
    Task<Topic?> GetByIdAsync(long id);
    Task<long> CreateAsync(Topic topic);
    Task UpdateAsync(Topic topic);
    Task DeleteAsync(long id);
}