using Forum.Api.Models.Entities;

namespace Forum.Api.Repositories;

public interface ITagRepository
{
    Task<IEnumerable<Tag>> GetAllAsync();
    Task<Tag?> GetByIdAsync(long id);
    Task<Tag?> GetBySlugAsync(string slug);
    Task<long> CreateAsync(Tag tag);
    Task UpdateAsync(Tag tag);
    Task DeleteAsync(long id);
}