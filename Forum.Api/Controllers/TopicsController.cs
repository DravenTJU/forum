using Microsoft.AspNetCore.Mvc;
using Forum.Api.Services;
using Forum.Api.Models.Entities;

namespace Forum.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    public async Task<IActionResult> GetAll([FromQuery] long? categoryId = null, [FromQuery] int limit = 20, [FromQuery] long? cursorId = null, [FromQuery] DateTime? cursorLastPosted = null)
    {
        var topics = await _topicService.GetAllAsync(categoryId, limit, cursorId, cursorLastPosted);
        return Ok(topics);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var topic = await _topicService.GetByIdAsync(id);
        if (topic == null)
        {
            return NotFound();
        }
        return Ok(topic);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTopicRequest request)
    {
        var slug = request.Title.ToLowerInvariant().Replace(" ", "-");
        var topicId = await _topicService.CreateAsync(request.Title, slug, request.AuthorId, request.CategoryId);
        return CreatedAtAction(nameof(GetById), new { id = topicId }, new { id = topicId });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateTopicRequest request)
    {
        try
        {
            var slug = request.Title.ToLowerInvariant().Replace(" ", "-");
            await _topicService.UpdateAsync(id, request.Title, slug, request.CategoryId);
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
            await _topicService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}

public class CreateTopicRequest
{
    public long CategoryId { get; set; }
    public long AuthorId { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class UpdateTopicRequest
{
    public string Title { get; set; } = string.Empty;
    public long CategoryId { get; set; }
}