using Forum.Api.Models.Entities;

namespace Forum.Api.Repositories;

public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(long id);
    Task<Category?> GetBySlugAsync(string slug);
    Task<long> CreateAsync(Category category);
    Task UpdateAsync(Category category);
    Task DeleteAsync(long id);
}