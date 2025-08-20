using Forum.Api.Infrastructure.Auth;
using Forum.Api.Models.Entities;
using Forum.Api.Models.DTOs;
using Forum.Api.Repositories;
using System.Security.Cryptography;
using System.Text;

namespace Forum.Api.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordService passwordService,
        IJwtTokenService tokenService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(string email, string password, string? userAgent = null, string? ipAddress = null)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (!_passwordService.VerifyPassword(password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (user.Status == UserStatus.Suspended)
        {
            throw new UnauthorizedAccessException("Account is suspended");
        }

        var roles = new[] { UserRole.User.ToString() };
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Username, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // 哈希并保存refresh token
        var refreshTokenHash = HashRefreshToken(refreshToken);
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // 7天过期
            UserAgent = userAgent,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.CreateAsync(refreshTokenEntity);

        _logger.LogInformation("User {UserId} logged in successfully", user.Id);
        
        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenType = "Bearer",
            ExpiresIn = 3600 // 1小时
        };
    }

    public async Task<long> RegisterAsync(string username, string email, string password)
    {
        var existingUser = await _userRepository.GetByEmailAsync(email);
        if (existingUser != null)
        {
            throw new ArgumentException("Email is already registered");
        }

        existingUser = await _userRepository.GetByUsernameAsync(username);
        if (existingUser != null)
        {
            throw new ArgumentException("Username is already taken");
        }

        var hashedPassword = _passwordService.HashPassword(password);

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = hashedPassword,
            Status = UserStatus.Active,
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var userId = await _userRepository.CreateAsync(user);
        _logger.LogInformation("User {UserId} registered successfully", userId);
        
        return userId;
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        return Task.FromResult(_tokenService.ValidateAccessToken(token));
    }

    public async Task<LoginResponse> RefreshTokenAsync(string refreshToken)
    {
        var refreshTokenHash = HashRefreshToken(refreshToken);
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(refreshTokenHash);
        
        if (storedToken == null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var user = await _userRepository.GetByIdAsync(storedToken.UserId);
        if (user == null || user.Status == UserStatus.Suspended)
        {
            throw new UnauthorizedAccessException("User not found or suspended");
        }

        // 撤销旧的refresh token
        await _refreshTokenRepository.RevokeAsync(storedToken.Id);

        // 生成新的tokens
        var roles = new[] { UserRole.User.ToString() };
        var newAccessToken = _tokenService.GenerateAccessToken(user.Id, user.Username, roles);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        // 保存新的refresh token
        var newRefreshTokenHash = HashRefreshToken(newRefreshToken);
        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newRefreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            UserAgent = storedToken.UserAgent,
            IpAddress = storedToken.IpAddress,
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.CreateAsync(newRefreshTokenEntity);

        _logger.LogInformation("Tokens refreshed for user {UserId}", user.Id);

        return new LoginResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            TokenType = "Bearer",
            ExpiresIn = 3600
        };
    }

    private static byte[] HashRefreshToken(string refreshToken)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
    }
}