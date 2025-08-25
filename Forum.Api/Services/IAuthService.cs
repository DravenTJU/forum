using Forum.Api.Models.DTOs;

namespace Forum.Api.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(string email, string password, string? userAgent = null, string? ipAddress = null);
    Task<long> RegisterAsync(string username, string email, string password);
    Task<bool> ValidateTokenAsync(string token);
    Task<LoginResponse> RefreshTokenAsync(string refreshToken);
    Task<UserDto> GetCurrentUserAsync(long userId);
}