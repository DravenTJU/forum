using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Forum.Api.Services;
using Forum.Api.Models.DTOs;
using System.Security.Claims;

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
                
                return BadRequest(ApiResponse.ErrorResult("VALIDATION_FAILED", "请求参数验证失败", errors));
            }

            if (!long.TryParse(topicId, out var topicIdLong))
            {
                return BadRequest(ApiResponse.ErrorResult("INVALID_TOPIC_ID", "无效的主题ID"));
            }

            // 验证主题是否存在
            var topic = await _topicService.GetByIdAsync(topicIdLong);
            if (topic == null)
            {
                return NotFound(ApiResponse.ErrorResult("TOPIC_NOT_FOUND", "主题不存在"));
            }

            var posts = await _postService.GetRepliesByTopicIdAsync(topicIdLong, query.Limit, null, null);
            var hasNext = posts.Count() >= query.Limit;
            var nextCursor = hasNext ? "next" : null;

            var postDtos = posts.Select(p => new PostDto
            {
                Id = p.Id.ToString(),
                TopicId = p.TopicId.ToString(),
                Author = new UserSummaryDto
                {
                    Id = p.AuthorId.ToString(),
                    Username = "user_" + p.AuthorId,
                    AvatarUrl = null
                },
                ContentMd = p.ContentMd,
                ContentHtml = "<p>" + p.ContentMd + "</p>",
                ReplyToPost = p.ReplyToPostId.HasValue ? new PostReferenceDto
                {
                    Id = p.ReplyToPostId.Value.ToString(),
                    Author = "user_" + p.ReplyToPostId.Value,
                    Excerpt = "引用内容..."
                } : null,
                Mentions = new string[0],
                IsEdited = p.IsEdited,
                IsDeleted = p.IsDeleted,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToArray();

            var meta = new ApiMeta
            {
                HasNext = hasNext,
                NextCursor = nextCursor
            };

            return Ok(ApiResponse<PostDto[]>.SuccessResult(postDtos, meta));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get posts for topic {TopicId}", topicId);
            return StatusCode(500, ApiResponse.ErrorResult("INTERNAL_ERROR", "获取帖子列表失败"));
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
                
                return BadRequest(ApiResponse.ErrorResult("VALIDATION_FAILED", "请求参数验证失败", errors));
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse.ErrorResult("INVALID_TOKEN", "无效的访问令牌"));
            }

            if (!long.TryParse(topicId, out var topicIdLong))
            {
                return BadRequest(ApiResponse.ErrorResult("INVALID_TOPIC_ID", "无效的主题ID"));
            }

            // 验证主题是否存在且未被锁定
            var topic = await _topicService.GetByIdAsync(topicIdLong);
            if (topic == null)
            {
                return NotFound(ApiResponse.ErrorResult("TOPIC_NOT_FOUND", "主题不存在"));
            }

            if (topic.IsLocked)
            {
                return BadRequest(ApiResponse.ErrorResult("TOPIC_LOCKED", "主题已被锁定，无法回帖"));
            }

            var authorId = long.Parse(userId);
            var replyToPostId = request.ReplyToPostId != null ? long.Parse(request.ReplyToPostId) : (long?)null;
            var postId = await _postService.CreateAsync(topicIdLong, authorId, request.ContentMd, replyToPostId);
            
            var post = await _postService.GetByIdAsync(postId);
            if (post == null)
            {
                return StatusCode(500, ApiResponse.ErrorResult("INTERNAL_ERROR", "创建帖子失败"));
            }

            var postDto = new PostDto
            {
                Id = post.Id.ToString(),
                TopicId = post.TopicId.ToString(),
                Author = new UserSummaryDto
                {
                    Id = post.AuthorId.ToString(),
                    Username = "user_" + post.AuthorId,
                    AvatarUrl = null
                },
                ContentMd = post.ContentMd,
                ContentHtml = "<p>" + post.ContentMd + "</p>",
                ReplyToPost = post.ReplyToPostId.HasValue ? new PostReferenceDto
                {
                    Id = post.ReplyToPostId.Value.ToString(),
                    Author = "user_" + post.ReplyToPostId.Value,
                    Excerpt = "引用内容..."
                } : null,
                Mentions = new string[0],
                IsEdited = post.IsEdited,
                IsDeleted = post.IsDeleted,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt
            };

            return CreatedAtAction(
                nameof(GetPostById),
                new { id = post.Id },
                ApiResponse<PostDto>.SuccessResult(postDto)
            );
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.ErrorResult("CREATE_POST_FAILED", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create post in topic {TopicId}", topicId);
            return StatusCode(500, ApiResponse.ErrorResult("INTERNAL_ERROR", "创建帖子失败"));
        }
    }

    [HttpGet("api/v1/posts/{id}")]
    public async Task<IActionResult> GetPostById(string id)
    {
        try
        {
            if (!long.TryParse(id, out var postId))
            {
                return BadRequest(ApiResponse.ErrorResult("INVALID_POST_ID", "无效的帖子ID"));
            }

            var post = await _postService.GetByIdAsync(postId);
            if (post == null)
            {
                return NotFound(ApiResponse.ErrorResult("POST_NOT_FOUND", "帖子不存在"));
            }

            var postDto = new PostDto
            {
                Id = post.Id.ToString(),
                TopicId = post.TopicId.ToString(),
                Author = new UserSummaryDto
                {
                    Id = post.AuthorId.ToString(),
                    Username = "user_" + post.AuthorId,
                    AvatarUrl = null
                },
                ContentMd = post.ContentMd,
                ContentHtml = "<p>" + post.ContentMd + "</p>",
                ReplyToPost = post.ReplyToPostId.HasValue ? new PostReferenceDto
                {
                    Id = post.ReplyToPostId.Value.ToString(),
                    Author = "user_" + post.ReplyToPostId.Value,
                    Excerpt = "引用内容..."
                } : null,
                Mentions = new string[0],
                IsEdited = post.IsEdited,
                IsDeleted = post.IsDeleted,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt
            };

            return Ok(ApiResponse<PostDto>.SuccessResult(postDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get post {PostId}", id);
            return StatusCode(500, ApiResponse.ErrorResult("INTERNAL_ERROR", "获取帖子详情失败"));
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
                
                return BadRequest(ApiResponse.ErrorResult("VALIDATION_FAILED", "请求参数验证失败", errors));
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse.ErrorResult("INVALID_TOKEN", "无效的访问令牌"));
            }

            if (!long.TryParse(id, out var postId))
            {
                return BadRequest(ApiResponse.ErrorResult("INVALID_POST_ID", "无效的帖子ID"));
            }

            var post = await _postService.GetByIdAsync(postId);
            if (post == null)
            {
                return NotFound(ApiResponse.ErrorResult("POST_NOT_FOUND", "帖子不存在"));
            }

            await _postService.UpdateAsync(postId, request.ContentMd);

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
            _logger.LogError(ex, "Failed to update post {PostId}", id);
            return StatusCode(500, ApiResponse.ErrorResult("INTERNAL_ERROR", "更新帖子失败"));
        }
    }

    [HttpDelete("api/v1/posts/{id}")]
    [Authorize]
    public async Task<IActionResult> DeletePost(string id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse.ErrorResult("INVALID_TOKEN", "无效的访问令牌"));
            }

            if (!long.TryParse(id, out var postId))
            {
                return BadRequest(ApiResponse.ErrorResult("INVALID_POST_ID", "无效的帖子ID"));
            }

            var post = await _postService.GetByIdAsync(postId);
            if (post == null)
            {
                return NotFound(ApiResponse.ErrorResult("POST_NOT_FOUND", "帖子不存在"));
            }

            await _postService.DeleteAsync(postId);

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, ApiResponse.ErrorResult("INSUFFICIENT_PERMISSIONS", "权限不足"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete post {PostId}", id);
            return StatusCode(500, ApiResponse.ErrorResult("INTERNAL_ERROR", "删除帖子失败"));
        }
    }
}