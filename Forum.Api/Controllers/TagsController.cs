using Microsoft.AspNetCore.Mvc;
using Forum.Api.Services;
using Forum.Api.Models.DTOs;

namespace Forum.Api.Controllers;

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
            var tags = await _tagService.GetAllAsync();
            
            var tagDtos = tags.Select(tag => new TagDto
            {
                Id = tag.Id.ToString(),
                Name = tag.Name,
                Slug = tag.Slug,
                Description = tag.Description,
                Color = tag.Color,
                TopicCount = tag.UsageCount
            }).ToArray();

            return Ok(ApiResponse<TagDto[]>.SuccessResult(tagDtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tags");
            return StatusCode(500, ApiResponse.ErrorResult("INTERNAL_ERROR", "获取标签列表失败"));
        }
    }
}