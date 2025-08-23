using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
                
                return BadRequest(ApiResponse.Error("VALIDATION_FAILED", "请求参数验证失败", errors));
            }

            await _authService.RegisterAsync(request.Username, request.Email, request.Password);
            return Ok(ApiResponse.Success(new { message = "注册成功，请查看邮箱验证邮件" }));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Error("REGISTRATION_FAILED", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for user {Username}", request.Username);
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "注册失败，请稍后重试"));
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
                
                return BadRequest(ApiResponse.Error("VALIDATION_FAILED", "请求参数验证失败", errors));
            }

            var userAgent = Request.Headers["User-Agent"].ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            
            var authResponse = await _authService.LoginAsync(request.Email, request.Password, userAgent, ipAddress);
            return Ok(ApiResponse<AuthResponse>.SuccessResult(authResponse));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse.Error("INVALID_CREDENTIALS", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for user {Email}", request.Email);
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "登录失败，请稍后重试"));
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse.Error("INVALID_TOKEN", "无效的访问令牌"));
            }

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse.Error("USER_NOT_FOUND", "用户不存在"));
            }

            return Ok(ApiResponse<UserDto>.SuccessResult(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current user");
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "获取用户信息失败"));
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await _authService.LogoutAsync(userId);
            }

            // 清除认证Cookie
            Response.Cookies.Delete("auth-token");
            Response.Cookies.Delete("csrf-token");

            return Ok(ApiResponse.Success(new { message = "退出成功" }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed");
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "退出失败"));
        }
    }

    [HttpPost("verify")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
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
                
                return BadRequest(ApiResponse.Error("VALIDATION_FAILED", "请求参数验证失败", errors));
            }

            await _authService.VerifyEmailAsync(request.Token);
            return Ok(ApiResponse.Success(new { message = "邮箱验证成功" }));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Error("VERIFICATION_FAILED", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email verification failed");
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "邮箱验证失败"));
        }
    }

    [HttpGet("csrf-token")]
    public IActionResult GetCsrfToken()
    {
        try
        {
            var token = Guid.NewGuid().ToString("N");
            
            // 设置CSRF Token到Cookie
            Response.Cookies.Append("csrf-token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromHours(24)
            });

            return Ok(ApiResponse.Success(new { csrfToken = token }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate CSRF token");
            return StatusCode(500, ApiResponse.Error("INTERNAL_ERROR", "获取CSRF令牌失败"));
        }
    }
}