namespace Forum.Api.Infrastructure.Auth;

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public int ExpirationInMinutes { get; set; }
    public int RefreshExpirationInDays { get; set; }
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}