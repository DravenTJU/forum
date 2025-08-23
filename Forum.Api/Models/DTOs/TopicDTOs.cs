using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Forum.Api.Models.DTOs;

public class TopicDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;
    
    [JsonPropertyName("author")]
    public UserSummaryDto Author { get; set; } = null!;
    
    [JsonPropertyName("category")]
    public CategorySummaryDto Category { get; set; } = null!;
    
    [JsonPropertyName("tags")]
    public TagDto[] Tags { get; set; } = Array.Empty<TagDto>();
    
    [JsonPropertyName("isPinned")]
    public bool IsPinned { get; set; }
    
    [JsonPropertyName("isLocked")]
    public bool IsLocked { get; set; }
    
    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; set; }
    
    [JsonPropertyName("replyCount")]
    public int ReplyCount { get; set; }
    
    [JsonPropertyName("viewCount")]
    public int ViewCount { get; set; }
    
    [JsonPropertyName("lastPostedAt")]
    public DateTime? LastPostedAt { get; set; }
    
    [JsonPropertyName("lastPoster")]
    public UserSummaryDto? LastPoster { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

public class TopicDetailDto : TopicDto
{
    [JsonPropertyName("firstPost")]
    public PostDto FirstPost { get; set; } = null!;
}

public class CreateTopicRequest
{
    [JsonPropertyName("title")]
    [Required(ErrorMessage = "标题不能为空")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "标题长度必须在5-200字符之间")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("contentMd")]
    [Required(ErrorMessage = "内容不能为空")]
    [StringLength(50000, MinimumLength = 10, ErrorMessage = "内容长度必须在10-50000字符之间")]
    public string ContentMd { get; set; } = string.Empty;
    
    [JsonPropertyName("categoryId")]
    [Required(ErrorMessage = "分类不能为空")]
    public string CategoryId { get; set; } = string.Empty;
    
    [JsonPropertyName("tagSlugs")]
    public string[] TagSlugs { get; set; } = Array.Empty<string>();
}

public class UpdateTopicRequest
{
    [JsonPropertyName("title")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "标题长度必须在5-200字符之间")]
    public string? Title { get; set; }
    
    [JsonPropertyName("categoryId")]
    public string? CategoryId { get; set; }
    
    [JsonPropertyName("tagSlugs")]
    public string[]? TagSlugs { get; set; }
    
    [JsonPropertyName("updatedAt")]
    [Required(ErrorMessage = "更新时间戳不能为空")]
    public DateTime UpdatedAt { get; set; }
}

public class TopicListQuery : PaginationQuery
{
    [JsonPropertyName("categoryId")]
    public string? CategoryId { get; set; }
    
    [JsonPropertyName("tag")]
    public string? Tag { get; set; }
    
    [JsonPropertyName("sort")]
    public string Sort { get; set; } = "latest";
}

public class MoveTopicRequest
{
    [JsonPropertyName("categoryId")]
    [Required(ErrorMessage = "目标分类不能为空")]
    public string CategoryId { get; set; } = string.Empty;
}

public class CategorySummaryDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;
}

public class TagDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("color")]
    public string? Color { get; set; }
    
    [JsonPropertyName("topicCount")]
    public int? TopicCount { get; set; }
}