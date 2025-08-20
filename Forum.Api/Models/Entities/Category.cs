namespace Forum.Api.Models.Entities;

public class Category
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#007acc";
    public int Order { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}