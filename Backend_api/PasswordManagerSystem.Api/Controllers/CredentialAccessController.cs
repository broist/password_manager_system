using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PasswordManagerSystem.Api.Application.DTOs.CredentialAccess;
using PasswordManagerSystem.Api.Application.Interfaces;
using PasswordManagerSystem.Api.Domain.Entities;
using PasswordManagerSystem.Api.Infrastructure.Data;

namespace PasswordManagerSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CredentialAccessController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IAuditService _auditService;

    public CredentialAccessController(
        AppDbContext dbContext,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccessRule([FromBody] CreateCredentialAccessRequest request)
    {
        var currentUserId = GetCurrentUserId();
        var currentRoleId = GetCurrentRoleId();
        var currentAdUsername = GetCurrentAdUsername();

        var isItAdmin = IsCurrentUserItAdmin();
        var isIt = IsCurrentUserIt();

        if (!isItAdmin && !isIt)
        {
            return Forbid();
        }

        if (request.CredentialId <= 0)
        {
            return BadRequest(new
            {
                message = "CredentialId is required."
            });
        }

        var hasRoleTarget = request.RoleId.HasValue;
        var hasUserTarget = request.UserId.HasValue;

        if (hasRoleTarget == hasUserTarget)
        {
            return BadRequest(new
            {
                message = "Exactly one of RoleId or UserId must be provided."
            });
        }

        if (!request.CanView && !request.CanWrite && !request.CanDelete)
        {
            return BadRequest(new
            {
                message = "At least one permission must be enabled."
            });
        }

        var credential = await _dbContext.Credentials
            .Include(x => x.Company)
            .FirstOrDefaultAsync(x => x.Id == request.CredentialId && x.IsActive);

        if (credential is null)
        {
            return NotFound(new
            {
                message = "Active credential not found."
            });
        }

        if (isIt && !isItAdmin)
        {
            var now = DateTime.UtcNow;

            var canCurrentItUserViewCredential = await _dbContext.CredentialAccesses
                .AsNoTracking()
                .AnyAsync(x =>
                    x.CredentialId == credential.Id &&
                    x.CanView &&
                    (x.ExpiresAt == null || x.ExpiresAt > now) &&
                    (
                        x.UserId == currentUserId ||
                        x.RoleId == currentRoleId
                    ));

            if (!canCurrentItUserViewCredential)
            {
                return Forbid();
            }

            if (!request.UserId.HasValue)
            {
                return BadRequest(new
                {
                    message = "IT users can only grant temporary access to a specific user. UserId is required."
                });
            }

            if (request.RoleId.HasValue)
            {
                return BadRequest(new
                {
                    message = "IT users cannot grant role-based access."
                });
            }

            if (request.ExpiresAt is null)
            {
                return BadRequest(new
                {
                    message = "IT users can only grant temporary access. ExpiresAt is required."
                });
            }

            if (request.ExpiresAt <= now)
            {
                return BadRequest(new
                {
                    message = "ExpiresAt must be in the future."
                });
            }

            if (request.CanWrite || request.CanDelete)
            {
                return BadRequest(new
                {
                    message = "IT users can only grant view access."
                });
            }

            request.CanView = true;
        }

        Role? role = null;
        User? targetUser = null;

        if (request.RoleId.HasValue)
        {
            role = await _dbContext.Roles
                .FirstOrDefaultAsync(x => x.Id == request.RoleId.Value && x.IsActive);

            if (role is null)
            {
                return NotFound(new
                {
                    message = "Active role not found."
                });
            }
        }

        if (request.UserId.HasValue)
        {
            targetUser = await _dbContext.Users
                .FirstOrDefaultAsync(x => x.Id == request.UserId.Value && x.IsActive);

            if (targetUser is null)
            {
                return NotFound(new
                {
                    message = "Active user not found."
                });
            }
        }

        var existingRule = await _dbContext.CredentialAccesses
            .FirstOrDefaultAsync(x =>
                x.CredentialId == request.CredentialId &&
                x.RoleId == request.RoleId &&
                x.UserId == request.UserId);

        if (existingRule is not null)
        {
            existingRule.CanView = request.CanView;
            existingRule.CanWrite = request.CanWrite;
            existingRule.CanDelete = request.CanDelete;
            existingRule.ExpiresAt = request.ExpiresAt;

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "CREDENTIAL_ACCESS_UPDATED",
                success: true,
                userId: currentUserId,
                adUsername: currentAdUsername,
                targetType: "CredentialAccess",
                targetId: existingRule.Id,
                credentialId: credential.Id,
                companyId: credential.CompanyId,
                targetUserId: request.UserId,
                details: BuildAuditDetails("Access rule updated", credential.Title, role, targetUser)
            );

            return Ok(ToResponse(existingRule, role, targetUser));
        }

        var accessRule = new CredentialAccess
        {
            CredentialId = request.CredentialId,
            RoleId = request.RoleId,
            UserId = request.UserId,
            CanView = request.CanView,
            CanWrite = request.CanWrite,
            CanDelete = request.CanDelete,
            ExpiresAt = request.ExpiresAt,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.CredentialAccesses.Add(accessRule);
        await _dbContext.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "CREDENTIAL_ACCESS_GRANTED",
            success: true,
            userId: currentUserId,
            adUsername: currentAdUsername,
            targetType: "CredentialAccess",
            targetId: accessRule.Id,
            credentialId: credential.Id,
            companyId: credential.CompanyId,
            targetUserId: request.UserId,
            details: BuildAuditDetails("Access rule granted", credential.Title, role, targetUser)
        );

        return CreatedAtAction(
            nameof(GetAccessRulesByCredential),
            new { credentialId = credential.Id },
            ToResponse(accessRule, role, targetUser)
        );
    }

    [HttpGet("credential/{credentialId:long}")]
    public async Task<IActionResult> GetAccessRulesByCredential(long credentialId)
    {
        if (!IsCurrentUserItAdmin())
        {
            return Forbid();
        }

        var credentialExists = await _dbContext.Credentials
            .AnyAsync(x => x.Id == credentialId);

        if (!credentialExists)
        {
            return NotFound(new
            {
                message = "Credential not found."
            });
        }

        var accessRules = await _dbContext.CredentialAccesses
            .AsNoTracking()
            .Include(x => x.Role)
            .Include(x => x.User)
            .Where(x => x.CredentialId == credentialId)
            .OrderBy(x => x.Id)
            .Select(x => new CredentialAccessResponse
            {
                Id = x.Id,
                CredentialId = x.CredentialId,
                RoleId = x.RoleId,
                RoleName = x.Role != null ? x.Role.Name : null,
                UserId = x.UserId,
                AdUsername = x.User != null ? x.User.AdUsername : null,
                CanView = x.CanView,
                CanWrite = x.CanWrite,
                CanDelete = x.CanDelete,
                ExpiresAt = x.ExpiresAt,
                CreatedByUserId = x.CreatedByUserId,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(accessRules);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteAccessRule(long id)
    {
        if (!IsCurrentUserItAdmin())
        {
            return Forbid();
        }

        var accessRule = await _dbContext.CredentialAccesses
            .Include(x => x.Credential)
            .ThenInclude(x => x.Company)
            .Include(x => x.Role)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (accessRule is null)
        {
            return NotFound(new
            {
                message = "Access rule not found."
            });
        }

        var currentUserId = GetCurrentUserId();
        var currentAdUsername = GetCurrentAdUsername();

        var credentialId = accessRule.CredentialId;
        var companyId = accessRule.Credential.CompanyId;
        var targetUserId = accessRule.UserId;
        var credentialTitle = accessRule.Credential.Title;
        var role = accessRule.Role;
        var user = accessRule.User;

        _dbContext.CredentialAccesses.Remove(accessRule);
        await _dbContext.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "CREDENTIAL_ACCESS_REVOKED",
            success: true,
            userId: currentUserId,
            adUsername: currentAdUsername,
            targetType: "CredentialAccess",
            targetId: id,
            credentialId: credentialId,
            companyId: companyId,
            targetUserId: targetUserId,
            details: BuildAuditDetails("Access rule revoked", credentialTitle, role, user)
        );

        return NoContent();
    }

    private static CredentialAccessResponse ToResponse(
        CredentialAccess accessRule,
        Role? role,
        User? user)
    {
        return new CredentialAccessResponse
        {
            Id = accessRule.Id,
            CredentialId = accessRule.CredentialId,
            RoleId = accessRule.RoleId,
            RoleName = role?.Name,
            UserId = accessRule.UserId,
            AdUsername = user?.AdUsername,
            CanView = accessRule.CanView,
            CanWrite = accessRule.CanWrite,
            CanDelete = accessRule.CanDelete,
            ExpiresAt = accessRule.ExpiresAt,
            CreatedByUserId = accessRule.CreatedByUserId,
            CreatedAt = accessRule.CreatedAt
        };
    }

    private static string BuildAuditDetails(
        string action,
        string credentialTitle,
        Role? role,
        User? user)
    {
        if (role is not null)
        {
            return $"{action}: {credentialTitle}, role: {role.Name}";
        }

        if (user is not null)
        {
            return $"{action}: {credentialTitle}, user: {user.AdUsername}";
        }

        return $"{action}: {credentialTitle}";
    }

    private long GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue("user_id");

        if (!long.TryParse(userIdValue, out var userId))
        {
            throw new InvalidOperationException("Authenticated user_id claim is missing or invalid.");
        }

        return userId;
    }

    private long GetCurrentRoleId()
    {
        var roleIdValue = User.FindFirstValue("role_id");

        if (!long.TryParse(roleIdValue, out var roleId))
        {
            throw new InvalidOperationException("Authenticated role_id claim is missing or invalid.");
        }

        return roleId;
    }

    private string GetCurrentAdUsername()
    {
        return User.FindFirstValue("ad_username") ?? "UNKNOWN";
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
}