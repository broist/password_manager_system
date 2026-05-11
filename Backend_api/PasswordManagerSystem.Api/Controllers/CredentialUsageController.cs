using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PasswordManagerSystem.Api.Application.DTOs.CredentialUsage;
using PasswordManagerSystem.Api.Application.Interfaces;
using PasswordManagerSystem.Api.Domain.Entities;
using PasswordManagerSystem.Api.Infrastructure.Data;

namespace PasswordManagerSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CredentialUsageController : ControllerBase
{
    private const string ActiveStatus = "ACTIVE";
    private const string EndedStatus = "ENDED";

    private readonly AppDbContext _dbContext;
    private readonly IAuditService _auditService;

    public CredentialUsageController(
        AppDbContext dbContext,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    [HttpGet("active/{credentialId:long}")]
    public async Task<IActionResult> GetActiveUsage(long credentialId)
    {
        var currentUserId = GetCurrentUserId();
        var currentRoleId = GetCurrentRoleId();

        var credential = await _dbContext.Credentials
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == credentialId && x.IsActive);

        if (credential is null)
        {
            return NotFound(new
            {
                message = "Active credential not found."
            });
        }

        if (!await CanCurrentUserViewCredentialAsync(credentialId, currentUserId, currentRoleId))
        {
            return Forbid();
        }

        var activeUsages = await _dbContext.CredentialUsageSessions
            .AsNoTracking()
            .Where(x =>
                x.CredentialId == credentialId &&
                x.Status == ActiveStatus &&
                x.EndedAt == null)
            .OrderBy(x => x.StartedAt)
            .Select(x => new ActiveCredentialUsageResponse
            {
                Id = x.Id,
                CredentialId = x.CredentialId,
                UserId = x.UserId,
                AdUsername = x.AdUsername,
                ConnectionValue = x.ConnectionValue,
                StartedAt = x.StartedAt,
                Status = x.Status
            })
            .ToListAsync();

        return Ok(activeUsages);
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartUsage([FromBody] StartCredentialUsageRequest request)
    {
        var currentUserId = GetCurrentUserId();
        var currentRoleId = GetCurrentRoleId();
        var currentAdUsername = GetCurrentAdUsername();

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

        if (!await CanCurrentUserViewCredentialAsync(credential.Id, currentUserId, currentRoleId))
        {
            return Forbid();
        }

        var hasActiveUsage = await _dbContext.CredentialUsageSessions
            .AsNoTracking()
            .AnyAsync(x =>
                x.CredentialId == credential.Id &&
                x.Status == ActiveStatus &&
                x.EndedAt == null);

        var usageSession = new CredentialUsageSession
        {
            CredentialId = credential.Id,
            UserId = currentUserId,
            AdUsername = currentAdUsername,
            ConnectionValue = request.ConnectionValue ?? credential.ConnectionValue,
            ProcessId = request.ProcessId,
            StartedAt = DateTime.UtcNow,
            EndedAt = null,
            Status = ActiveStatus
        };

        _dbContext.CredentialUsageSessions.Add(usageSession);
        await _dbContext.SaveChangesAsync();

        await _auditService.LogAsync(
            action: hasActiveUsage && request.OverrideActiveUsage
                ? "CREDENTIAL_USAGE_OVERRIDE_STARTED"
                : "CREDENTIAL_USAGE_STARTED",
            success: true,
            userId: currentUserId,
            adUsername: currentAdUsername,
            targetType: "CredentialUsageSession",
            targetId: usageSession.Id,
            credentialId: credential.Id,
            companyId: credential.CompanyId,
            details: $"Credential usage started: {credential.Title}"
        );

        return Ok(new StartCredentialUsageResponse
        {
            Id = usageSession.Id,
            CredentialId = usageSession.CredentialId,
            StartedAt = usageSession.StartedAt,
            Status = usageSession.Status
        });
    }

    [HttpPost("end/{id:long}")]
    public async Task<IActionResult> EndUsage(long id, [FromBody] EndCredentialUsageRequest? request)
    {
        var currentUserId = GetCurrentUserId();
        var currentAdUsername = GetCurrentAdUsername();

        var usageSession = await _dbContext.CredentialUsageSessions
            .Include(x => x.Credential)
            .ThenInclude(x => x.Company)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (usageSession is null)
        {
            return NotFound(new
            {
                message = "Credential usage session not found."
            });
        }

        var isOwner = usageSession.UserId == currentUserId;
        var isItAdmin = IsCurrentUserItAdmin();

        if (!isOwner && !isItAdmin)
        {
            return Forbid();
        }

        if (usageSession.Status == EndedStatus || usageSession.EndedAt is not null)
        {
            return NoContent();
        }

        usageSession.Status = EndedStatus;
        usageSession.EndedAt = DateTime.UtcNow;

        if (request?.ProcessId is not null)
        {
            usageSession.ProcessId = request.ProcessId;
        }

        await _dbContext.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "CREDENTIAL_USAGE_ENDED",
            success: true,
            userId: currentUserId,
            adUsername: currentAdUsername,
            targetType: "CredentialUsageSession",
            targetId: usageSession.Id,
            credentialId: usageSession.CredentialId,
            companyId: usageSession.Credential.CompanyId,
            details: $"Credential usage ended: {usageSession.Credential.Title}"
        );

        return NoContent();
    }

    private async Task<bool> CanCurrentUserViewCredentialAsync(
        long credentialId,
        long currentUserId,
        long currentRoleId)
    {
        if (IsCurrentUserItAdmin())
        {
            return true;
        }

        var now = DateTime.UtcNow;

        return await _dbContext.CredentialAccesses
            .AsNoTracking()
            .AnyAsync(x =>
                x.CredentialId == credentialId &&
                x.CanView &&
                (x.ExpiresAt == null || x.ExpiresAt > now) &&
                (
                    x.UserId == currentUserId ||
                    x.RoleId == currentRoleId
                ));
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
}