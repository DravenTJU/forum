namespace Forum.Api.Models.Entities;

public class Topic
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public long AuthorId { get; set; }
    public long CategoryId { get; set; }
    public bool IsPinned { get; set; }
    public bool IsLocked { get; set; }
    public bool IsDeleted { get; set; }
    public int ReplyCount { get; set; }
    public int ViewCount { get; set; }
    public DateTime? LastPostedAt { get; set; }
    public long? LastPosterId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}