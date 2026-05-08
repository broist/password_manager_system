using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PasswordManagerSystem.Api.Infrastructure.Data;

namespace PasswordManagerSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public HealthController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        var databaseAvailable = await _dbContext.Database.CanConnectAsync();

        return Ok(new
        {
            status = databaseAvailable ? "Healthy" : "Degraded",
            api = "PasswordManagerSystem.Api",
            database = databaseAvailable ? "Available" : "Unavailable",
            utcTime = DateTime.UtcNow
        });
    }
}