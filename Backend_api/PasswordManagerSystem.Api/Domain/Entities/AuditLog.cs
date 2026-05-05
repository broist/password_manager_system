namespace PasswordManagerSystem.Api.Domain.Entities;

public class AuditLog
{
    public long Id { get; set; }

    public long? UserId { get; set; }

    public User? User { get; set; }

    public string AdUsername { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string? TargetType { get; set; }

    public long? TargetId { get; set; }

    public long? CredentialId { get; set; }

    public Credential? Credential { get; set; }

    public long? CompanyId { get; set; }

    public Company? Company { get; set; }

    public long? TargetUserId { get; set; }

    public User? TargetUser { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public bool Success { get; set; } = true;

    public string? Details { get; set; }

    public string? PreviousHash { get; set; }

    public string Hash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}