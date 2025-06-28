using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class HomeController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        var serverTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
        return Ok($"✅ Server is up and running as of {serverTime}");
    }
}
