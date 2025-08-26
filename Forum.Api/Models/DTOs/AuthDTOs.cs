using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Forum.Api.Models.DTOs;

public class RegisterRequest
{
    [JsonPropertyName("username")]
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "用户名长度必须在3-50字符之间")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "密码长度至少8位")]
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    [JsonPropertyName("email")]
    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    [JsonPropertyName("user")]
    public UserDto User { get; set; } = null!;
    
    [JsonPropertyName("csrfToken")]
    public string CsrfToken { get; set; } = string.Empty;
}

public class VerifyEmailRequest
{
    [JsonPropertyName("token")]
    [Required(ErrorMessage = "验证令牌不能为空")]
    public string Token { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    [JsonPropertyName("email")]
    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [JsonPropertyName("token")]
    [Required(ErrorMessage = "重置令牌不能为空")]
    public string Token { get; set; } = string.Empty;
    
    [JsonPropertyName("password")]
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "密码长度至少8位")]
    public string Password { get; set; } = string.Empty;
}