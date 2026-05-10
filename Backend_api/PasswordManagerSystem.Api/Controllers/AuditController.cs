using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PasswordManagerSystem.Api.Application.DTOs.Audit;
using PasswordManagerSystem.Api.Application.Interfaces;
using PasswordManagerSystem.Api.Infrastructure.Data;

namespace PasswordManagerSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IAuditChainVerifierService _auditChainVerifierService;

    public AuditController(
        AppDbContext dbContext,
        IAuditChainVerifierService auditChainVerifierService)
    {
        _dbContext = dbContext;
        _auditChainVerifierService = auditChainVerifierService;
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int take = 200,
        [FromQuery] string? action = null,
        [FromQuery] string? adUsername = null,
        [FromQuery] long? credentialId = null,
        [FromQuery] long? companyId = null,
        [FromQuery] bool? success = null)
    {
        if (!IsCurrentUserItAdmin())
        {
            return Forbid();
        }

        take = Math.Clamp(take, 1, 1000);

        var query = _dbContext.AuditLogs
            .AsNoTracking()
            .Include(x => x.Credential)
            .Include(x => x.Company)
            .Include(x => x.TargetUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(x => x.Action.Contains(action));
        }

        if (!string.IsNullOrWhiteSpace(adUsername))
        {
            query = query.Where(x => x.AdUsername.Contains(adUsername));
        }

        if (credentialId.HasValue)
        {
            query = query.Where(x => x.CredentialId == credentialId.Value);
        }

        if (companyId.HasValue)
        {
            query = query.Where(x => x.CompanyId == companyId.Value);
        }

        if (success.HasValue)
        {
            query = query.Where(x => x.Success == success.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.Id)
            .Take(take)
            .Select(x => new AuditLogResponse
            {
                Id = x.Id,
                UserId = x.UserId,
                AdUsername = x.AdUsername,
                Action = x.Action,
                TargetType = x.TargetType,
                TargetId = x.TargetId,
                CredentialId = x.CredentialId,
                CredentialTitle = x.Credential != null ? x.Credential.Title : null,
                CompanyId = x.CompanyId,
                CompanyName = x.Company != null ? x.Company.Name : null,
                TargetUserId = x.TargetUserId,
                TargetAdUsername = x.TargetUser != null ? x.TargetUser.AdUsername : null,
                IpAddress = x.IpAddress,
                Success = x.Success,
                Details = x.Details,
                PreviousHash = x.PreviousHash,
                Hash = x.Hash,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(new AuditLogListResponse
        {
            Items = items,
            TotalCount = totalCount,
            ReturnedCount = items.Count
        });
    }

    [HttpGet("verify-chain")]
    public async Task<IActionResult> VerifyChain()
    {
        if (!IsCurrentUserItAdmin())
        {
            return Forbid();
        }

        var result = await _auditChainVerifierService.VerifyAsync();

        return Ok(result);
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