using Microsoft.AspNetCore.Mvc;
using PasswordManagerSystem.Api.Application.DTOs;
using PasswordManagerSystem.Api.Application.DTOs.Auth;
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
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IConfiguration _configuration;

    public AuthController(
        IAdAuthenticationService adAuthenticationService,
        IRoleResolverService roleResolverService,
        IUserSyncService userSyncService,
        IJwtTokenService jwtTokenService,
        IAuditService auditService,
        IRefreshTokenService refreshTokenService,
        IConfiguration configuration)
    {
        _adAuthenticationService = adAuthenticationService;
        _roleResolverService = roleResolverService;
        _userSyncService = userSyncService;
        _jwtTokenService = jwtTokenService;
        _auditService = auditService;
        _refreshTokenService = refreshTokenService;
        _configuration = configuration;
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

        var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(
            user,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        await _auditService.LogAsync(
            action: "LOGIN_SUCCESS",
            success: true,
            userId: user.Id,
            adUsername: user.AdUsername,
            targetType: "User",
            targetId: user.Id
        );

        return Ok(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenType = "Bearer",
            ExpiresInMinutes = GetAccessTokenMinutes(),
            User = new UserInfoResponse
            {
                Id = user.Id,
                AdUsername = user.AdUsername,
                DisplayName = user.DisplayName ?? user.AdUsername,
                Email = user.Email,
                RoleId = user.RoleId,
                IsActive = user.IsActive,
                FirstLoginAt = user.FirstLoginAt,
                LastLoginAt = user.LastLoginAt,
                RoleSyncedAt = user.RoleSyncedAt
            },
            Role = new RoleInfoResponse
            {
                Id = resolvedRole.Id,
                Name = resolvedRole.Name,
                DisplayName = resolvedRole.DisplayName ?? resolvedRole.Name,
                AdGroupName = resolvedRole.AdGroupName,
                Level = resolvedRole.Level
            },
            Groups = adUser.Groups
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var existingRefreshToken = await _refreshTokenService.GetActiveRefreshTokenAsync(
            request.RefreshToken
        );

        if (existingRefreshToken is null)
        {
            return Unauthorized(new
            {
                message = "Invalid or expired refresh token."
            });
        }

        var user = existingRefreshToken.User;

        if (!user.IsActive)
        {
            return Unauthorized(new
            {
                message = "User is inactive."
            });
        }

        var role = await _roleResolverService.ResolveHighestRoleAsync(
            new[] { user.Role.AdGroupName }
        );

        if (role is null)
        {
            return Unauthorized(new
            {
                message = "User has no valid application role."
            });
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(user, role);

        var newRefreshToken = await _refreshTokenService.CreateRefreshTokenAsync(
            user,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        var newRefreshTokenHash = _refreshTokenService.HashRefreshToken(newRefreshToken);

        await _refreshTokenService.RevokeRefreshTokenAsync(
            existingRefreshToken,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            newRefreshTokenHash
        );

        await _auditService.LogAsync(
            action: "TOKEN_REFRESHED",
            success: true,
            userId: user.Id,
            adUsername: user.AdUsername,
            targetType: "User",
            targetId: user.Id
        );

        return Ok(new RefreshTokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            TokenType = "Bearer",
            ExpiresInMinutes = GetAccessTokenMinutes()
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var existingRefreshToken = await _refreshTokenService.GetActiveRefreshTokenAsync(
            request.RefreshToken
        );

        if (existingRefreshToken is null)
        {
            return NoContent();
        }

        await _refreshTokenService.RevokeRefreshTokenAsync(
            existingRefreshToken,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        await _auditService.LogAsync(
            action: "LOGOUT",
            success: true,
            userId: existingRefreshToken.UserId,
            adUsername: existingRefreshToken.User.AdUsername,
            targetType: "User",
            targetId: existingRefreshToken.UserId
        );

        return NoContent();
    }

    private int GetAccessTokenMinutes()
    {
        return int.Parse(_configuration["Jwt:AccessTokenMinutes"] ?? "60");
    }
}