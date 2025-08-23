using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Forum.Api.Services;
using Forum.Api.Models.DTOs;

namespace Forum.Api.Controllers;

[ApiController]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly ITopicService _topicService;
    private readonly ILogger<PostsController> _logger;

    public PostsController(IPostService postService, ITopicService topicService, ILogger<PostsController> logger)
    {
        _postService = postService;
        _topicService = topicService;
        _logger = logger;
    }

    [HttpGet("api/v1/topics/{topicId}/posts")]
    public async Task<IActionResult> GetPostsByTopic(string topicId, [FromQuery] PostListQuery query)
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

            // 验证主题是否存在
            var topicExists = await _topicService.TopicExistsAsync(topicId);
            if (!topicExists)
            {
                return NotFound(ApiResponse.Error("TOPIC_NOT_FOUND", "主题不存在"));
            }

            var (posts, hasNext, nextCursor) = await _postService.GetPostsByTopicAsync(
                topicId,
                query.Cursor,
                query.Limit
            );

            var meta = new ApiMeta
            {
                HasNext = hasNext,
                NextCursor = nextCursor
            };

            return Ok(ApiResponse<PostDto[]>.SuccessResult(posts, meta));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get posts for topic {TopicId}", topicId);
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "获取帖子列表失败"));
        }
    }

    [HttpPost("api/v1/topics/{topicId}/posts")]
    [Authorize]
    public async Task<IActionResult> CreatePost(string topicId, [FromBody] CreatePostRequest request)
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

            // 验证主题是否存在且未被锁定
            var topic = await _topicService.GetTopicByIdAsync(topicId);
            if (topic == null)
            {
                return NotFound(ApiResponse.Error("TOPIC_NOT_FOUND", "主题不存在"));
            }

            if (topic.IsLocked)
            {
                return BadRequest(ApiResponse.Error("TOPIC_LOCKED", "主题已被锁定，无法回帖"));
            }

            var post = await _postService.CreatePostAsync(
                topicId,
                userId,
                request.ContentMd,
                request.ReplyToPostId
            );

            return CreatedAtAction(
                nameof(GetPostById),
                new { id = post.Id },
                ApiResponse<PostDto>.SuccessResult(post)
            );
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Error("CREATE_POST_FAILED", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create post in topic {TopicId}", topicId);
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "创建帖子失败"));
        }
    }

    [HttpGet("api/v1/posts/{id}")]
    public async Task<IActionResult> GetPostById(string id)
    {
        try
        {
            var post = await _postService.GetPostByIdAsync(id);
            if (post == null)
            {
                return NotFound(ApiResponse.Error("POST_NOT_FOUND", "帖子不存在"));
            }

            return Ok(ApiResponse<PostDto>.SuccessResult(post));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get post {PostId}", id);
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "获取帖子详情失败"));
        }
    }

    [HttpPatch("api/v1/posts/{id}")]
    [Authorize]
    public async Task<IActionResult> UpdatePost(string id, [FromBody] UpdatePostRequest request)
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

            var success = await _postService.UpdatePostAsync(
                id,
                userId,
                request.ContentMd,
                request.UpdatedAt
            );

            if (!success)
            {
                return NotFound(ApiResponse.Error("POST_NOT_FOUND", "帖子不存在或无权限修改"));
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
            _logger.LogError(ex, "Failed to update post {PostId}", id);
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "更新帖子失败"));
        }
    }

    [HttpDelete("api/v1/posts/{id}")]
    [Authorize]
    public async Task<IActionResult> DeletePost(string id)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse.Error("INVALID_TOKEN", "无效的访问令牌"));
            }

            var success = await _postService.DeletePostAsync(id, userId);
            if (!success)
            {
                return NotFound(ApiResponse.Error("POST_NOT_FOUND", "帖子不存在或无权限删除"));
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid(ApiResponse.Error("INSUFFICIENT_PERMISSIONS", "权限不足"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete post {PostId}", id);
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "删除帖子失败"));
        }
    }
}