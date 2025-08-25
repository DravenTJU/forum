using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Forum.Api.Services;
using Forum.Api.Models.DTOs;

namespace Forum.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    );
                
                return BadRequest(ApiResponse.ErrorResult("VALIDATION_FAILED", "请求参数验证失败", errors));
            }

            await _authService.RegisterAsync(request.Username, request.Email, request.Password);
            return Ok(ApiResponse.SuccessResult(new { message = "注册成功，请查看邮箱验证邮件" }));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.ErrorResult("REGISTRATION_FAILED", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for user {Username}", request.Username);
            return StatusCode(500, ApiResponse.ErrorResult("INTERNAL_ERROR", "注册失败，请稍后重试"));
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    );
                
                return BadRequest(ApiResponse.ErrorResult("VALIDATION_FAILED", "请求参数验证失败", errors));
            }

            var userAgent = Request.Headers["User-Agent"].ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            
            var loginResponse = await _authService.LoginAsync(request.Email, request.Password, userAgent, ipAddress);
            
            // 设置JWT Token到HttpOnly Cookie (符合API规范)
            var isDevelopment = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps && !isDevelopment, // 开发环境允许HTTP
                SameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.Strict, // 开发环境使用Lax
                Expires = DateTimeOffset.UtcNow.AddSeconds(loginResponse.ExpiresIn),
                Path = "/"
            };
            Response.Cookies.Append("auth-token", loginResponse.AccessToken, cookieOptions);
            
            // 刷新令牌存储在单独的HttpOnly Cookie
            var refreshCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps && !isDevelopment,
                SameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                Path = "/"
            };
            Response.Cookies.Append("refresh-token", loginResponse.RefreshToken, refreshCookieOptions);

            // 返回成功响应（不包含敏感token信息）
            return Ok(ApiResponse.SuccessResult(new { message = "登录成功" }));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse.ErrorResult("INVALID_CREDENTIALS", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for user {Email}", request.Email);
            return StatusCode(500, ApiResponse.ErrorResult("INTERNAL_ERROR", "登录失败，请稍后重试"));
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            // 清除认证Cookie
            Response.Cookies.Delete("auth-token");
            Response.Cookies.Delete("csrf-token");

            return Ok(ApiResponse.SuccessResult(new { message = "退出成功" }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed");
            return StatusCode(500, ApiResponse.ErrorResult("INTERNAL_ERROR", "退出失败"));
        }
    }

    [HttpGet("csrf-token")]
    public IActionResult GetCsrfToken()
    {
        try
        {
            var token = Guid.NewGuid().ToString("N");
            
            // 设置CSRF Token到Cookie
            var isDevelopment = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();
            Response.Cookies.Append("csrf-token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps && !isDevelopment,
                SameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.Strict,
                MaxAge = TimeSpan.FromHours(24),
                Path = "/"
            });

            return Ok(ApiResponse.SuccessResult(new { csrfToken = token }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate CSRF token");
            return StatusCode(500, ApiResponse.ErrorResult("INTERNAL_ERROR", "获取CSRF令牌失败"));
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse.ErrorResult("INVALID_TOKEN", "无效的认证令牌"));
            }

            var user = await _authService.GetCurrentUserAsync(userId);
            return Ok(ApiResponse<UserDto>.SuccessResult(user));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse.ErrorResult("UNAUTHORIZED", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get current user failed for userId {UserId}", User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
            return StatusCode(500, ApiResponse.ErrorResult("INTERNAL_ERROR", "获取用户信息失败"));
        }
    }

    [HttpGet("test-auth")]
    [Authorize]
    public IActionResult TestAuth()
    {
        try
        {
            var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToArray();
            
            return Ok(ApiResponse.SuccessResult(new 
            { 
                userId, 
                username, 
                claims 
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test auth failed");
            return StatusCode(500, ApiResponse.ErrorResult("INTERNAL_ERROR", "测试认证失败"));
        }
    }
}