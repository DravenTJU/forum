using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Forum.Api.Models.DTOs;

public class PostDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("topicId")]
    public string TopicId { get; set; } = string.Empty;
    
    [JsonPropertyName("author")]
    public UserSummaryDto Author { get; set; } = null!;
    
    [JsonPropertyName("contentMd")]
    public string ContentMd { get; set; } = string.Empty;
    
    [JsonPropertyName("contentHtml")]
    public string? ContentHtml { get; set; }
    
    [JsonPropertyName("replyToPost")]
    public PostReferenceDto? ReplyToPost { get; set; }
    
    [JsonPropertyName("mentions")]
    public string[] Mentions { get; set; } = Array.Empty<string>();
    
    [JsonPropertyName("isEdited")]
    public bool IsEdited { get; set; }
    
    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

public class PostReferenceDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;
    
    [JsonPropertyName("excerpt")]
    public string Excerpt { get; set; } = string.Empty;
}

public class CreatePostRequest
{
    [JsonPropertyName("contentMd")]
    [Required(ErrorMessage = "帖子内容不能为空")]
    [StringLength(50000, MinimumLength = 5, ErrorMessage = "帖子内容长度必须在5-50000字符之间")]
    public string ContentMd { get; set; } = string.Empty;
    
    [JsonPropertyName("replyToPostId")]
    public string? ReplyToPostId { get; set; }
}

public class UpdatePostRequest
{
    [JsonPropertyName("contentMd")]
    [Required(ErrorMessage = "帖子内容不能为空")]
    [StringLength(50000, MinimumLength = 5, ErrorMessage = "帖子内容长度必须在5-50000字符之间")]
    public string ContentMd { get; set; } = string.Empty;
    
    [JsonPropertyName("updatedAt")]
    [Required(ErrorMessage = "更新时间戳不能为空")]
    public DateTime UpdatedAt { get; set; }
}

public class PostListQuery : PaginationQuery
{
    // 继承自PaginationQuery的Cursor和Limit属性
}