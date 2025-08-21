using Microsoft.AspNetCore.Mvc;
using Forum.Api.Infrastructure.Auth;

namespace Forum.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly IPasswordService _passwordService;

    public TestController(IPasswordService passwordService)
    {
        _passwordService = passwordService;
    }

    [HttpPost("password")]
    public IActionResult TestPassword([FromBody] TestPasswordRequest request)
    {
        var hash = _passwordService.HashPassword(request.Password);
        var isValid = _passwordService.VerifyPassword(request.Password, hash);
        
        return Ok(new { 
            originalPassword = request.Password,
            hash = hash,
            isValid = isValid 
        });
    }
}

public class TestPasswordRequest
{
    public string Password { get; set; } = string.Empty;
}