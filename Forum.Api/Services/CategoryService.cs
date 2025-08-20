using Forum.Api.Models.Entities;
using Forum.Api.Repositories;

namespace Forum.Api.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        return await _categoryRepository.GetAllAsync();
    }

    public async Task<Category?> GetByIdAsync(long id)
    {
        return await _categoryRepository.GetByIdAsync(id);
    }

    public async Task<Category?> GetBySlugAsync(string slug)
    {
        return await _categoryRepository.GetBySlugAsync(slug);
    }
}