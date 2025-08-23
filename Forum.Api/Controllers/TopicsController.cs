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
                
                return BadRequest(ApiResponse.Error("VALIDATION_FAILED", "请求参数验证失败", errors));
            }

            var (topics, hasNext, nextCursor) = await _topicService.GetTopicsAsync(
                query.CategoryId, 
                query.Tag, 
                query.Sort, 
                query.Cursor, 
                query.Limit
            );

            var meta = new ApiMeta
            {
                HasNext = hasNext,
                NextCursor = nextCursor
            };

            return Ok(ApiResponse<TopicDto[]>.SuccessResult(topics, meta));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get topics list");
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "获取主题列表失败"));
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTopicById(string id)
    {
        try
        {
            var topic = await _topicService.GetTopicByIdAsync(id);
            if (topic == null)
            {
                return NotFound(ApiResponse.Error("TOPIC_NOT_FOUND", "主题不存在"));
            }

            // 增加浏览计数
            await _topicService.IncrementViewCountAsync(id);

            return Ok(ApiResponse<TopicDetailDto>.SuccessResult(topic));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get topic {TopicId}", id);
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "获取主题详情失败"));
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
                
                return BadRequest(ApiResponse.Error("VALIDATION_FAILED", "请求参数验证失败", errors));
            }

            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse.Error("INVALID_TOKEN", "无效的访问令牌"));
            }

            var topic = await _topicService.CreateTopicAsync(
                request.Title,
                request.ContentMd,
                userId,
                request.CategoryId,
                request.TagSlugs
            );

            return CreatedAtAction(
                nameof(GetTopicById),
                new { id = topic.Id },
                ApiResponse<TopicDetailDto>.SuccessResult(topic)
            );
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Error("CREATE_TOPIC_FAILED", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create topic");
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "创建主题失败"));
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
                
                return BadRequest(ApiResponse.Error("VALIDATION_FAILED", "请求参数验证失败", errors));
            }

            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse.Error("INVALID_TOKEN", "无效的访问令牌"));
            }

            var success = await _topicService.UpdateTopicAsync(
                id,
                userId,
                request.Title,
                request.CategoryId,
                request.TagSlugs,
                request.UpdatedAt
            );

            if (!success)
            {
                return NotFound(ApiResponse.Error("TOPIC_NOT_FOUND", "主题不存在或无权限修改"));
            }

            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("conflict"))
        {
            return Conflict(ApiResponse.Error("CONFLICT", "数据已被其他用户修改，请刷新后重试"));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid(ApiResponse.Error("INSUFFICIENT_PERMISSIONS", "权限不足"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update topic {TopicId}", id);
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "更新主题失败"));
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
                return Unauthorized(ApiResponse.Error("INVALID_TOKEN", "无效的访问令牌"));
            }

            var success = await _topicService.DeleteTopicAsync(id, userId);
            if (!success)
            {
                return NotFound(ApiResponse.Error("TOPIC_NOT_FOUND", "主题不存在或无权限删除"));
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid(ApiResponse.Error("INSUFFICIENT_PERMISSIONS", "权限不足"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete topic {TopicId}", id);
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "删除主题失败"));
        }
    }

    [HttpPost("{id}/pin")]
    [Authorize]
    public async Task<IActionResult> PinTopic(string id)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse.Error("INVALID_TOKEN", "无效的访问令牌"));
            }

            var success = await _topicService.PinTopicAsync(id, userId);
            if (!success)
            {
                return NotFound(ApiResponse.Error("TOPIC_NOT_FOUND", "主题不存在或无权限置顶"));
            }

            return Ok(ApiResponse.Success(new { message = "主题置顶成功" }));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid(ApiResponse.Error("INSUFFICIENT_PERMISSIONS", "权限不足"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pin topic {TopicId}", id);
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "置顶主题失败"));
        }
    }

    [HttpPost("{id}/lock")]
    [Authorize]
    public async Task<IActionResult> LockTopic(string id)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse.Error("INVALID_TOKEN", "无效的访问令牌"));
            }

            var success = await _topicService.LockTopicAsync(id, userId);
            if (!success)
            {
                return NotFound(ApiResponse.Error("TOPIC_NOT_FOUND", "主题不存在或无权限锁定"));
            }

            return Ok(ApiResponse.Success(new { message = "主题锁定成功" }));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid(ApiResponse.Error("INSUFFICIENT_PERMISSIONS", "权限不足"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to lock topic {TopicId}", id);
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "锁定主题失败"));
        }
    }

    [HttpPost("{id}/move")]
    [Authorize]
    public async Task<IActionResult> MoveTopic(string id, [FromBody] MoveTopicRequest request)
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

            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse.Error("INVALID_TOKEN", "无效的访问令牌"));
            }

            var success = await _topicService.MoveTopicAsync(id, request.CategoryId, userId);
            if (!success)
            {
                return NotFound(ApiResponse.Error("TOPIC_NOT_FOUND", "主题不存在或无权限移动"));
            }

            return Ok(ApiResponse.Success(new { message = "主题移动成功" }));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid(ApiResponse.Error("INSUFFICIENT_PERMISSIONS", "权限不足"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move topic {TopicId}", id);
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "移动主题失败"));
        }
    }
}