using Forum.Api.Models.Entities;

namespace Forum.Api.Services;

public interface ITopicService
{
    Task<IEnumerable<Topic>> GetAllAsync(long? categoryId = null, int limit = 20, long? cursorId = null, DateTime? cursorLastPosted = null);
    Task<Topic?> GetByIdAsync(long id);
    Task<long> CreateAsync(string title, string slug, long authorId, long categoryId);
    Task UpdateAsync(long id, string title, string slug, long categoryId);
    Task DeleteAsync(long id);
}