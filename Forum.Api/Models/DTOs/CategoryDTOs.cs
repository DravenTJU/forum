using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Forum.Api.Models.DTOs;

public class CategoryDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("order")]
    public int Order { get; set; }
    
    [JsonPropertyName("isArchived")]
    public bool IsArchived { get; set; }
    
    [JsonPropertyName("topicCount")]
    public int? TopicCount { get; set; }
}

public class CreateCategoryRequest
{
    [JsonPropertyName("name")]
    [Required(ErrorMessage = "分类名称不能为空")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "分类名称长度必须在2-100字符之间")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("slug")]
    [Required(ErrorMessage = "分类别名不能为空")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "分类别名长度必须在2-50字符之间")]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "分类别名只能包含小写字母、数字和连字符")]
    public string Slug { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    [StringLength(500, ErrorMessage = "分类描述不能超过500字符")]
    public string? Description { get; set; }
    
    [JsonPropertyName("order")]
    [Range(0, int.MaxValue, ErrorMessage = "排序必须为非负数")]
    public int Order { get; set; } = 0;
}

public class UpdateCategoryRequest
{
    [JsonPropertyName("name")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "分类名称长度必须在2-100字符之间")]
    public string? Name { get; set; }
    
    [JsonPropertyName("description")]
    [StringLength(500, ErrorMessage = "分类描述不能超过500字符")]
    public string? Description { get; set; }
    
    [JsonPropertyName("order")]
    [Range(0, int.MaxValue, ErrorMessage = "排序必须为非负数")]
    public int? Order { get; set; }
    
    [JsonPropertyName("isArchived")]
    public bool? IsArchived { get; set; }
}