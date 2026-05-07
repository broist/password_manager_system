using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PasswordManagerSystem.Api.Application.Interfaces;
using PasswordManagerSystem.Api.Domain.Entities;
using PasswordManagerSystem.Api.Infrastructure.Data;

namespace PasswordManagerSystem.Api.Application.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(
        AppDbContext dbContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        string action,
        bool success,
        long? userId = null,
        string? adUsername = null,
        string? targetType = null,
        long? targetId = null,
        long? credentialId = null,
        long? companyId = null,
        long? targetUserId = null,
        string? details = null
    )
    {
        var lastHash = await _dbContext.AuditLogs
            .OrderByDescending(x => x.Id)
            .Select(x => x.Hash)
            .FirstOrDefaultAsync();

        var ipAddress = _httpContextAccessor.HttpContext?
            .Connection
            .RemoteIpAddress?
            .ToString();

        var userAgent = _httpContextAccessor.HttpContext?
            .Request
            .Headers
            .UserAgent
            .ToString();

        var createdAt = DateTime.UtcNow;

        var auditLog = new AuditLog
        {
            UserId = userId,
            AdUsername = adUsername ?? "UNKNOWN",
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            CredentialId = credentialId,
            CompanyId = companyId,
            TargetUserId = targetUserId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Success = success,
            Details = details,
            PreviousHash = lastHash,
            CreatedAt = createdAt,
            Hash = string.Empty
        };

        auditLog.Hash = ComputeHash(auditLog);

        _dbContext.AuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync();
    }

    private static string ComputeHash(AuditLog log)
    {
        var rawData = string.Join("|",
            log.PreviousHash ?? string.Empty,
            log.UserId?.ToString() ?? string.Empty,
            log.AdUsername,
            log.Action,
            log.TargetType ?? string.Empty,
            log.TargetId?.ToString() ?? string.Empty,
            log.CredentialId?.ToString() ?? string.Empty,
            log.CompanyId?.ToString() ?? string.Empty,
            log.TargetUserId?.ToString() ?? string.Empty,
            log.IpAddress ?? string.Empty,
            log.UserAgent ?? string.Empty,
            log.Success.ToString(),
            log.Details ?? string.Empty,
            log.CreatedAt.ToString("O")
        );

        var bytes = Encoding.UTF8.GetBytes(rawData);
        var hashBytes = SHA256.HashData(bytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}