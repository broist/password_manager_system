namespace PasswordManagerSystem.Api.Application.DTOs.Audit;

public class AuditChainVerificationResponse
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