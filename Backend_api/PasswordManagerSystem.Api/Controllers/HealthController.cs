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

    [HttpGet("database")]
    public async Task<IActionResult> CheckDatabase()
    {
        var canConnect = await _dbContext.Database.CanConnectAsync();

        if (!canConnect)
        {
            return StatusCode(503, new
            {
                status = "unhealthy",
                database = "unreachable"
            });
        }

        var roleCount = await _dbContext.Roles.CountAsync();

        return Ok(new
        {
            status = "healthy",
            database = "reachable",
            roles = roleCount
        });
    }
}