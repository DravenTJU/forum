namespace Forum.Api.Models.Entities;

public class RefreshToken
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public byte[] TokenHash { get; set; } = new byte[0];
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}