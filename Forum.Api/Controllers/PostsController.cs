using Microsoft.AspNetCore.Mvc;
using Forum.Api.Services;
using Forum.Api.Models.Entities;

namespace Forum.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly ILogger<PostsController> _logger;

    public PostsController(IPostService postService, ILogger<PostsController> logger)
    {
        _postService = postService;
        _logger = logger;
    }

    [HttpGet("topic/{topicId}")]
    public async Task<IActionResult> GetByTopic(long topicId, [FromQuery] int limit = 20, [FromQuery] long? cursorId = null, [FromQuery] DateTime? cursorCreated = null)
    {
        var posts = await _postService.GetByTopicIdAsync(topicId, limit, cursorId, cursorCreated);
        return Ok(posts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var post = await _postService.GetByIdAsync(id);
        if (post == null)
        {
            return NotFound();
        }
        return Ok(post);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePostRequest request)
    {
        var postId = await _postService.CreateAsync(request.TopicId, request.AuthorId, request.ContentMd, request.ReplyToPostId);
        return CreatedAtAction(nameof(GetById), new { id = postId }, new { id = postId });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdatePostRequest request)
    {
        try
        {
            await _postService.UpdateAsync(id, request.ContentMd);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            await _postService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}

public class CreatePostRequest
{
    public long TopicId { get; set; }
    public long AuthorId { get; set; }
    public string ContentMd { get; set; } = string.Empty;
    public long? ReplyToPostId { get; set; }
}

public class UpdatePostRequest
{
    public string ContentMd { get; set; } = string.Empty;
}