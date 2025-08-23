using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Forum.Api.Services;
using Forum.Api.Models.DTOs;

namespace Forum.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(ICategoryService categoryService, ILogger<CategoriesController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        try
        {
            var categories = await _categoryService.GetAllAsync();
            var categoryDtos = categories.Select(c => new CategoryDto
            {
                Id = c.Id.ToString(),
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                Order = c.Order,
                IsArchived = c.IsArchived,
                TopicCount = 0
            }).ToArray();

            return Ok(ApiResponse<CategoryDto[]>.SuccessResult(categoryDtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get categories list");
            return StatusCode(500, ApiResponse.ErrorResult("INTERNAL_ERROR", "获取分类列表失败"));
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategoryById(string id)
    {
        try
        {
            if (!long.TryParse(id, out var categoryId))
            {
                return BadRequest(ApiResponse.ErrorResult("INVALID_CATEGORY_ID", "无效的分类ID"));
            }

            var category = await _categoryService.GetByIdAsync(categoryId);
            if (category == null)
            {
                return NotFound(ApiResponse.ErrorResult("CATEGORY_NOT_FOUND", "分类不存在"));
            }

            var categoryDto = new CategoryDto
            {
                Id = category.Id.ToString(),
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                Order = category.Order,
                IsArchived = category.IsArchived,
                TopicCount = 0
            };

            return Ok(ApiResponse<CategoryDto>.SuccessResult(categoryDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get category {CategoryId}", id);
            return StatusCode(500, ApiResponse.ErrorResult("INTERNAL_ERROR", "获取分类详情失败"));
        }
    }

    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetCategoryBySlug(string slug)
    {
        try
        {
            var category = await _categoryService.GetBySlugAsync(slug);
            if (category == null)
            {
                return NotFound(ApiResponse.ErrorResult("CATEGORY_NOT_FOUND", "分类不存在"));
            }

            var categoryDto = new CategoryDto
            {
                Id = category.Id.ToString(),
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                Order = category.Order,
                IsArchived = category.IsArchived,
                TopicCount = 0
            };

            return Ok(ApiResponse<CategoryDto>.SuccessResult(categoryDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get category {CategorySlug}", slug);
            return StatusCode(500, ApiResponse.ErrorResult("INTERNAL_ERROR", "获取分类详情失败"));
        }
    }
}