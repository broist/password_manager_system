using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PasswordManagerSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SecureTestController : ControllerBase
{
    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirstValue("user_id");
        var adUsername = User.FindFirstValue("ad_username");
        var roleId = User.FindFirstValue("role_id");
        var roleName = User.FindFirstValue("role_name");
        var roleLevel = User.FindFirstValue("role_level");

        return Ok(new
        {
            message = "JWT token is valid.",
            userId,
            adUsername,
            roleId,
            roleName,
            roleLevel
        });
    }
}