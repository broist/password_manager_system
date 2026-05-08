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
    private readonly IAuditService _auditService;

    public AuthController(
        IAdAuthenticationService adAuthenticationService,
        IRoleResolverService roleResolverService,
        IUserSyncService userSyncService,
        IJwtTokenService jwtTokenService,
        IAuditService auditService)
    {
        _adAuthenticationService = adAuthenticationService;
        _roleResolverService = roleResolverService;
        _userSyncService = userSyncService;
        _jwtTokenService = jwtTokenService;
        _auditService = auditService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var adUser = await _adAuthenticationService.AuthenticateAsync(
            request.Username,
            request.Password
        );

        if (adUser is null)
        {
            await _auditService.LogAsync(
                action: "LOGIN_FAILED",
                success: false,
                adUsername: request.Username,
                details: "Invalid username, password, or AD group membership."
            );

            return Unauthorized(new
            {
                message = "Invalid username, password, or AD group membership."
            });
        }

        var resolvedRole = await _roleResolverService.ResolveHighestRoleAsync(adUser.Groups);

        if (resolvedRole is null)
        {
            await _auditService.LogAsync(
                action: "LOGIN_FAILED",
                success: false,
                adUsername: adUser.AdUsername,
                details: "User has no valid application role."
            );

            return Unauthorized(new
            {
                message = "User has no valid application role."
            });
        }

        var user = await _userSyncService.SyncUserAsync(adUser, resolvedRole);

        await _auditService.LogAsync(
            action: "ROLE_SYNCED",
            success: true,
            userId: user.Id,
            adUsername: user.AdUsername,
            targetType: "User",
            targetId: user.Id,
            details: $"Resolved role: {resolvedRole.Name}"
        );

        var accessToken = _jwtTokenService.GenerateAccessToken(user, resolvedRole);

        await _auditService.LogAsync(
            action: "LOGIN_SUCCESS",
            success: true,
            userId: user.Id,
            adUsername: user.AdUsername,
            targetType: "User",
            targetId: user.Id
        );

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