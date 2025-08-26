using System.Text.Json.Serialization;

namespace Forum.Api.Models.DTOs;

public class UserDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("emailVerified")]
    public bool EmailVerified { get; set; }
    
    [JsonPropertyName("avatarUrl")]
    public string? AvatarUrl { get; set; }
    
    [JsonPropertyName("bio")]
    public string? Bio { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = "active";
    
    [JsonPropertyName("roles")]
    public string[] Roles { get; set; } = new[] { "user" };
    
    [JsonPropertyName("lastSeenAt")]
    public DateTime? LastSeenAt { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

public class UserProfileDto : UserDto
{
    [JsonPropertyName("topicCount")]
    public int TopicCount { get; set; }
    
    [JsonPropertyName("postCount")]
    public int PostCount { get; set; }
}

public class UserSummaryDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonPropertyName("avatarUrl")]
    public string? AvatarUrl { get; set; }
}