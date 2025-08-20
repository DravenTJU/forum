namespace Forum.Api.Infrastructure.Auth;

public interface IJwtTokenService
{
    string GenerateAccessToken(long userId, string username, IEnumerable<string> roles);
    string GenerateRefreshToken();
    bool ValidateAccessToken(string token);
}