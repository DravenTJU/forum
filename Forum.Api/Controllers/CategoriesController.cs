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
    private readonly ITagService _tagService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(ICategoryService categoryService, ITagService tagService, ILogger<CategoriesController> logger)
    {
        _categoryService = categoryService;
        _tagService = tagService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        try
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return Ok(ApiResponse<CategoryDto[]>.SuccessResult(categories));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get categories list");
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "获取分类列表失败"));
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategoryById(string id)
    {
        try
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound(ApiResponse.Error("CATEGORY_NOT_FOUND", "分类不存在"));
            }

            return Ok(ApiResponse<CategoryDto>.SuccessResult(category));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get category {CategoryId}", id);
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "获取分类详情失败"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    );
                
                return BadRequest(ApiResponse.Error("VALIDATION_FAILED", "请求参数验证失败", errors));
            }

            var category = await _categoryService.CreateCategoryAsync(
                request.Name,
                request.Slug,
                request.Description,
                request.Order
            );

            return CreatedAtAction(
                nameof(GetCategoryById),
                new { id = category.Id },
                ApiResponse<CategoryDto>.SuccessResult(category)
            );
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Error("CREATE_CATEGORY_FAILED", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create category");
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "创建分类失败"));
        }
    }

    [HttpPatch("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateCategory(string id, [FromBody] UpdateCategoryRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    );
                
                return BadRequest(ApiResponse.Error("VALIDATION_FAILED", "请求参数验证失败", errors));
            }

            var success = await _categoryService.UpdateCategoryAsync(
                id,
                request.Name,
                request.Description,
                request.Order,
                request.IsArchived
            );

            if (!success)
            {
                return NotFound(ApiResponse.Error("CATEGORY_NOT_FOUND", "分类不存在"));
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid(ApiResponse.Error("INSUFFICIENT_PERMISSIONS", "权限不足"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update category {CategoryId}", id);
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "更新分类失败"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteCategory(string id)
    {
        try
        {
            var success = await _categoryService.DeleteCategoryAsync(id);
            if (!success)
            {
                return NotFound(ApiResponse.Error("CATEGORY_NOT_FOUND", "分类不存在"));
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Error("DELETE_CATEGORY_FAILED", ex.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid(ApiResponse.Error("INSUFFICIENT_PERMISSIONS", "权限不足"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete category {CategoryId}", id);
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "删除分类失败"));
        }
    }
}

[ApiController]
[Route("api/v1/tags")]
public class TagsController : ControllerBase
{
    private readonly ITagService _tagService;
    private readonly ILogger<TagsController> _logger;

    public TagsController(ITagService tagService, ILogger<TagsController> logger)
    {
        _tagService = tagService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetTags()
    {
        try
        {
            var tags = await _tagService.GetAllTagsAsync();
            return Ok(ApiResponse<TagDto[]>.SuccessResult(tags));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tags list");
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "获取标签列表失败"));
        }
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetTagBySlug(string slug)
    {
        try
        {
            var tag = await _tagService.GetTagBySlugAsync(slug);
            if (tag == null)
            {
                return NotFound(ApiResponse.Error("TAG_NOT_FOUND", "标签不存在"));
            }

            return Ok(ApiResponse<TagDto>.SuccessResult(tag));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tag {TagSlug}", slug);
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "获取标签详情失败"));
        }
    }
}