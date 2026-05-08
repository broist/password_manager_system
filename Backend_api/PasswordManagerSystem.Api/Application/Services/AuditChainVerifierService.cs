using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PasswordManagerSystem.Api.Application.DTOs.Audit;
using PasswordManagerSystem.Api.Application.Interfaces;
using PasswordManagerSystem.Api.Domain.Entities;
using PasswordManagerSystem.Api.Infrastructure.Data;

namespace PasswordManagerSystem.Api.Application.Services;

public class AuditChainVerifierService : IAuditChainVerifierService
{
    private readonly AppDbContext _dbContext;

    public AuditChainVerifierService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuditChainVerificationResponse> VerifyAsync()
    {
        var logs = await _dbContext.AuditLogs
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync();

        if (logs.Count == 0)
        {
            return new AuditChainVerificationResponse
            {
                IsValid = true,
                CheckedRecords = 0,
                Message = "Audit log is empty."
            };
        }

        string? previousHash = null;
        var checkedRecords = 0;

        foreach (var log in logs)
        {
            checkedRecords++;

            if (log.PreviousHash != previousHash)
            {
                return new AuditChainVerificationResponse
                {
                    IsValid = false,
                    CheckedRecords = checkedRecords,
                    BrokenAtAuditLogId = log.Id,
                    ExpectedPreviousHash = previousHash,
                    ActualPreviousHash = log.PreviousHash,
                    Message = "Audit hash-chain previous_hash validation failed."
                };
            }

            var expectedHash = ComputeHash(log);

            if (!string.Equals(expectedHash, log.Hash, StringComparison.OrdinalIgnoreCase))
            {
                return new AuditChainVerificationResponse
                {
                    IsValid = false,
                    CheckedRecords = checkedRecords,
                    BrokenAtAuditLogId = log.Id,
                    ExpectedHash = expectedHash,
                    ActualHash = log.Hash,
                    Message = "Audit hash-chain hash validation failed."
                };
            }

            previousHash = log.Hash;
        }

        return new AuditChainVerificationResponse
        {
            IsValid = true,
            CheckedRecords = checkedRecords,
            Message = "Audit hash-chain is valid."
        };
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
            FormatDateTime(log.CreatedAt)
        );

        var bytes = Encoding.UTF8.GetBytes(rawData);
        var hashBytes = SHA256.HashData(bytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
    private static DateTime TruncateToSecond(DateTime dateTime)
    {
        return new DateTime(
            dateTime.Year,
            dateTime.Month,
            dateTime.Day,
            dateTime.Hour,
            dateTime.Minute,
            dateTime.Second,
            DateTimeKind.Unspecified
        );
    }

    private static string FormatDateTime(DateTime dateTime)
    {
        return TruncateToSecond(dateTime)
            .ToString("yyyy-MM-ddTHH:mm:ss");
    }
}