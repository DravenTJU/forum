using Forum.Api.Models.Entities;

namespace Forum.Api.Services;

public interface ICategoryService
{
    Task<IEnumerable<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(long id);
    Task<Category?> GetBySlugAsync(string slug);
}