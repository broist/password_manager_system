namespace PasswordManagerSystem.Client.Models.Audit;

public sealed class AuditChainVerificationResponse
{
    public bool IsValid { get; set; }

    public int CheckedRecords { get; set; }

    public long? BrokenAtAuditLogId { get; set; }

    public string? ExpectedPreviousHash { get; set; }

    public string? ActualPreviousHash { get; set; }

    public string? ExpectedHash { get; set; }

    public string? ActualHash { get; set; }

    public string Message { get; set; } = string.Empty;
}

public sealed class AuditLogResponse
{
    public long Id { get; set; }

    public long? UserId { get; set; }

    public string AdUsername { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string? TargetType { get; set; }

    public long? TargetId { get; set; }

    public long? CredentialId { get; set; }

    public string? CredentialTitle { get; set; }

    public long? CompanyId { get; set; }

    public string? CompanyName { get; set; }

    public long? TargetUserId { get; set; }

    public string? TargetAdUsername { get; set; }

    public string? IpAddress { get; set; }

    public bool Success { get; set; }

    public string? Details { get; set; }

    public string? PreviousHash { get; set; }

    public string Hash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public sealed class AuditLogListResponse
{
    public IReadOnlyList<AuditLogResponse> Items { get; set; } = Array.Empty<AuditLogResponse>();

    public int TotalCount { get; set; }

    public int ReturnedCount { get; set; }
}