using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PasswordManagerSystem.Api.Infrastructure.Data;

namespace PasswordManagerSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public UsersController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveUsers()
    {
        if (!IsCurrentUserItAdmin() && !IsCurrentUserIt())
        {
            return Forbid();
        }

        var users = await _dbContext.Users
            .AsNoTracking()
            .Include(x => x.Role)
            .Where(x => x.IsActive && x.Role.IsActive)
            .OrderBy(x => x.AdUsername)
            .Select(x => new ActiveUserResponse
            {
                Id = x.Id,
                AdUsername = x.AdUsername,
                DisplayName = x.DisplayName,
                Email = x.Email,
                RoleId = x.RoleId,
                RoleName = x.Role.Name,
                RoleDisplayName = x.Role.DisplayName
            })
            .ToListAsync();

        return Ok(users);
    }

    private string GetCurrentRoleName()
    {
        return User.FindFirstValue("role_name") ?? string.Empty;
    }

    private bool IsCurrentUserItAdmin()
    {
        return string.Equals(
            GetCurrentRoleName(),
            "ITAdmin",
            StringComparison.OrdinalIgnoreCase
        );
    }

    private bool IsCurrentUserIt()
    {
        return string.Equals(
            GetCurrentRoleName(),
            "IT",
            StringComparison.OrdinalIgnoreCase
        );
    }

    private sealed class ActiveUserResponse
    {
        public long Id { get; set; }

        public string AdUsername { get; set; } = string.Empty;

        public string? DisplayName { get; set; }

        public string? Email { get; set; }

        public long RoleId { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public string RoleDisplayName { get; set; } = string.Empty;
    }
}