using Microsoft.AspNetCore.Mvc;
using PasswordManagerSystem.Api.Application.DTOs;
using PasswordManagerSystem.Api.Application.Interfaces;

namespace PasswordManagerSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAdAuthenticationService _adAuthenticationService;

    public AuthController(IAdAuthenticationService adAuthenticationService)
    {
        _adAuthenticationService = adAuthenticationService;
    }

    [HttpPost("login-test")]
    public async Task<IActionResult> LoginTest([FromBody] LoginRequest request)
    {
        var adUser = await _adAuthenticationService.AuthenticateAsync(
            request.Username,
            request.Password
        );

        if (adUser is null)
        {
            return Unauthorized(new
            {
                message = "Invalid username, password, or AD group membership."
            });
        }

        return Ok(new
        {
            message = "Mock AD authentication successful.",
            adUser.AdUsername,
            adUser.DisplayName,
            adUser.Email,
            adUser.Groups
        });
    }
}