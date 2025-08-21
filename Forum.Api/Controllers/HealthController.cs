using Microsoft.AspNetCore.Mvc;

namespace Forum.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { 
            status = "healthy", 
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }

    [HttpGet("db")]
    public async Task<IActionResult> CheckDatabase([FromServices] Infrastructure.Database.IDbConnectionFactory connectionFactory)
    {
        try
        {
            using var connection = await connectionFactory.CreateConnectionAsync();
            return Ok(new { 
                database = "connected", 
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                database = "disconnected", 
                error = ex.Message,
                timestamp = DateTime.UtcNow 
            });
        }
    }
}