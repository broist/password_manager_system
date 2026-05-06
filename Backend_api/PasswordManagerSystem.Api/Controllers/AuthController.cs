using Microsoft.AspNetCore.Mvc;
using PasswordManagerSystem.Api.Application.DTOs;
using PasswordManagerSystem.Api.Application.Interfaces;

namespace PasswordManagerSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAdAuthenticationService _adAuthenticationService;
    private readonly IRoleResolverService _roleResolverService;
    private readonly IUserSyncService _userSyncService;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(
        IAdAuthenticationService adAuthenticationService,
        IRoleResolverService roleResolverService,
        IUserSyncService userSyncService,
        IJwtTokenService jwtTokenService)
    {
        _adAuthenticationService = adAuthenticationService;
        _roleResolverService = roleResolverService;
        _userSyncService = userSyncService;
        _jwtTokenService = jwtTokenService;
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

        var resolvedRole = await _roleResolverService.ResolveHighestRoleAsync(adUser.Groups);

        if (resolvedRole is null)
        {
            return Unauthorized(new
            {
                message = "User has no valid application role."
            });
        }

        var user = await _userSyncService.SyncUserAsync(adUser, resolvedRole);
        var accessToken = _jwtTokenService.GenerateAccessToken(user, resolvedRole);

        return Ok(new
        {
            message = "Mock AD authentication successful.",
            accessToken,
            tokenType = "Bearer",
            expiresInMinutes = 60,
            user = new
            {
                user.Id,
                user.AdUsername,
                user.DisplayName,
                user.Email,
                user.RoleId,
                user.IsActive,
                user.FirstLoginAt,
                user.LastLoginAt,
                user.RoleSyncedAt
            },
            role = new
            {
                resolvedRole.Id,
                resolvedRole.Name,
                resolvedRole.DisplayName,
                resolvedRole.AdGroupName,
                resolvedRole.Level
            },
            groups = adUser.Groups
        });
    }
}