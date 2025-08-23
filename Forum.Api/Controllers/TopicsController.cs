using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Forum.Api.Services;
using Forum.Api.Models.DTOs;

namespace Forum.Api.Controllers;

[ApiController]
[Route("api/v1/topics")]
public class TopicsController : ControllerBase
{
    private readonly ITopicService _topicService;
    private readonly ILogger<TopicsController> _logger;

    public TopicsController(ITopicService topicService, ILogger<TopicsController> logger)
    {
        _topicService = topicService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetTopics([FromQuery] TopicListQuery query)
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
                
                return BadRequest(ApiResponse.ErrorResult("VALIDATION_FAILED", "请求参数验证失败", errors));
            }

            var categoryId = query.CategoryId != null ? long.Parse(query.CategoryId) : (long?)null;
            var topics = await _topicService.GetAllAsync(categoryId, query.Limit, null, null);
            var hasNext = topics.Count() >= query.Limit;
            var nextCursor = hasNext ? "next" : null;

            // 转换为DTO格式
            var topicDtos = topics.Select(t => new TopicDto
            {
                Id = t.Id.ToString(),
                Title = t.Title,
                Slug = t.Slug,
                Author = new UserSummaryDto
                {
                    Id = t.AuthorId.ToString(),
                    Username = "user_" + t.AuthorId,
                    AvatarUrl = null
                },
                Category = new CategorySummaryDto
                {
                    Id = t.CategoryId.ToString(),
                    Name = "Category " + t.CategoryId,
                    Slug = "category-" + t.CategoryId
                },
                Tags = new TagDto[0],
                IsPinned = t.IsPinned,
                IsLocked = t.IsLocked,
                IsDeleted = t.IsDeleted,
                ReplyCount = 0,
                ViewCount = t.ViewCount,
                LastPostedAt = t.LastPostedAt,
                LastPoster = null,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            }).ToArray();

            var meta = new ApiMeta
            {
                HasNext = hasNext,
                NextCursor = nextCursor
            };

            return Ok(ApiResponse<TopicDto[]>.SuccessResult(topicDtos, meta));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get topics list");
            return StatusCode(500, ApiResponse.ErrorResult("INTERNAL_ERROR", "获取主题列表失败"));
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTopicById(string id)
    {
        try
        {
            if (!long.TryParse(id, out var topicId))
            {
                return BadRequest(ApiResponse.ErrorResult("INVALID_TOPIC_ID", "无效的主题ID"));
            }

            var topic = await _topicService.GetByIdAsync(topicId);
            if (topic == null)
            {
                return NotFound(ApiResponse.ErrorResult("TOPIC_NOT_FOUND", "主题不存在"));
            }

            var topicDetailDto = new TopicDetailDto
            {
                Id = topic.Id.ToString(),
                Title = topic.Title,
                Slug = topic.Slug,
                Author = new UserSummaryDto
                {
                    Id = topic.AuthorId.ToString(),
                    Username = "user_" + topic.AuthorId,
                    AvatarUrl = null
                },
                Category = new CategorySummaryDto
                {
                    Id = topic.CategoryId.ToString(),
                    Name = "Category " + topic.CategoryId,
                    Slug = "category-" + topic.CategoryId
                },
                Tags = new TagDto[0],
                IsPinned = topic.IsPinned,
                IsLocked = topic.IsLocked,
                IsDeleted = topic.IsDeleted,
                ReplyCount = 0,
                ViewCount = topic.ViewCount,
                LastPostedAt = topic.LastPostedAt,
                LastPoster = null,
                CreatedAt = topic.CreatedAt,
                UpdatedAt = topic.UpdatedAt,
                FirstPost = new PostDto
                {
                    Id = "p_" + topic.Id,
                    TopicId = topic.Id.ToString(),
                    Author = new UserSummaryDto
                    {
                        Id = topic.AuthorId.ToString(),
                        Username = "user_" + topic.AuthorId,
                        AvatarUrl = null
                    },
                    ContentMd = "# " + topic.Title + "\n\n主题内容...",
                    ContentHtml = "<h1>" + topic.Title + "</h1><p>主题内容...</p>",
                    ReplyToPost = null,
                    Mentions = new string[0],
                    IsEdited = false,
                    IsDeleted = false,
                    CreatedAt = topic.CreatedAt,
                    UpdatedAt = topic.UpdatedAt
                }
            };

            return Ok(ApiResponse<TopicDetailDto>.SuccessResult(topicDetailDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get topic {TopicId}", id);
            return StatusCode(500, ApiResponse.ErrorResult("INTERNAL_ERROR", "获取主题详情失败"));
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateTopic([FromBody] CreateTopicRequest request)
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
                
                return BadRequest(ApiResponse.ErrorResult("VALIDATION_FAILED", "请求参数验证失败", errors));
            }

            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse.ErrorResult("INVALID_TOKEN", "无效的访问令牌"));
            }

            var slug = request.Title.ToLowerInvariant().Replace(" ", "-");
            var categoryId = long.Parse(request.CategoryId);
            var authorId = long.Parse(userId);
            var topicId = await _topicService.CreateAsync(request.Title, slug, authorId, categoryId);
            
            var topic = await _topicService.GetByIdAsync(topicId);
            if (topic == null)
            {
                return StatusCode(500, ApiResponse.ErrorResult("INTERNAL_ERROR", "创建主题失败"));
            }

            var topicDetailDto = new TopicDetailDto
            {
                Id = topic.Id.ToString(),
                Title = topic.Title,
                Slug = topic.Slug,
                Author = new UserSummaryDto
                {
                    Id = topic.AuthorId.ToString(),
                    Username = "user_" + topic.AuthorId,
                    AvatarUrl = null
                },
                Category = new CategorySummaryDto
                {
                    Id = topic.CategoryId.ToString(),
                    Name = "Category " + topic.CategoryId,
                    Slug = "category-" + topic.CategoryId
                },
                Tags = new TagDto[0],
                IsPinned = topic.IsPinned,
                IsLocked = topic.IsLocked,
                IsDeleted = topic.IsDeleted,
                ReplyCount = 0,
                ViewCount = topic.ViewCount,
                LastPostedAt = topic.LastPostedAt,
                LastPoster = null,
                CreatedAt = topic.CreatedAt,
                UpdatedAt = topic.UpdatedAt,
                FirstPost = new PostDto
                {
                    Id = "p_" + topic.Id,
                    TopicId = topic.Id.ToString(),
                    Author = new UserSummaryDto
                    {
                        Id = topic.AuthorId.ToString(),
                        Username = "user_" + topic.AuthorId,
                        AvatarUrl = null
                    },
                    ContentMd = request.ContentMd,
                    ContentHtml = "<p>" + request.ContentMd + "</p>",
                    ReplyToPost = null,
                    Mentions = new string[0],
                    IsEdited = false,
                    IsDeleted = false,
                    CreatedAt = topic.CreatedAt,
                    UpdatedAt = topic.UpdatedAt
                }
            };

            return CreatedAtAction(
                nameof(GetTopicById),
                new { id = topic.Id },
                ApiResponse<TopicDetailDto>.SuccessResult(topicDetailDto)
            );
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.ErrorResult("CREATE_TOPIC_FAILED", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create topic");
            return StatusCode(500, ApiResponse.ErrorResult("INTERNAL_ERROR", "创建主题失败"));
        }
    }

    [HttpPatch("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateTopic(string id, [FromBody] UpdateTopicRequest request)
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
                
                return BadRequest(ApiResponse.ErrorResult("VALIDATION_FAILED", "请求参数验证失败", errors));
            }

            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse.ErrorResult("INVALID_TOKEN", "无效的访问令牌"));
            }

            if (!long.TryParse(id, out var topicId))
            {
                return BadRequest(ApiResponse.ErrorResult("INVALID_TOPIC_ID", "无效的主题ID"));
            }

            var topic = await _topicService.GetByIdAsync(topicId);
            if (topic == null)
            {
                return NotFound(ApiResponse.ErrorResult("TOPIC_NOT_FOUND", "主题不存在"));
            }

            var slug = request.Title?.ToLowerInvariant().Replace(" ", "-") ?? topic.Slug;
            var categoryId = request.CategoryId != null ? long.Parse(request.CategoryId) : topic.CategoryId;
            
            await _topicService.UpdateAsync(topicId, request.Title ?? topic.Title, slug, categoryId);

            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("conflict"))
        {
            return Conflict(ApiResponse.ErrorResult("CONFLICT", "数据已被其他用户修改，请刷新后重试"));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, ApiResponse.ErrorResult("INSUFFICIENT_PERMISSIONS", "权限不足"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update topic {TopicId}", id);
            return StatusCode(500, ApiResponse.ErrorResult("INTERNAL_ERROR", "更新主题失败"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteTopic(string id)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse.ErrorResult("INVALID_TOKEN", "无效的访问令牌"));
            }

            if (!long.TryParse(id, out var topicId))
            {
                return BadRequest(ApiResponse.ErrorResult("INVALID_TOPIC_ID", "无效的主题ID"));
            }

            var topic = await _topicService.GetByIdAsync(topicId);
            if (topic == null)
            {
                return NotFound(ApiResponse.ErrorResult("TOPIC_NOT_FOUND", "主题不存在"));
            }

            await _topicService.DeleteAsync(topicId);

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, ApiResponse.ErrorResult("INSUFFICIENT_PERMISSIONS", "权限不足"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete topic {TopicId}", id);
            return StatusCode(500, ApiResponse.ErrorResult("INTERNAL_ERROR", "删除主题失败"));
        }
    }

    // TODO: 实现主题管理操作 (pin, lock, move) 在相关服务添加方法后
}