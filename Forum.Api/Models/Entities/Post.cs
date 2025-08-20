namespace Forum.Api.Models.Entities;

public class Post
{
    public long Id { get; set; }
    public long TopicId { get; set; }
    public long AuthorId { get; set; }
    public string ContentMd { get; set; } = string.Empty;
    public long? ReplyToPostId { get; set; }
    public bool IsEdited { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}