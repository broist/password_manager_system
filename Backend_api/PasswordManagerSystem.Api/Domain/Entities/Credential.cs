namespace PasswordManagerSystem.Api.Domain.Entities;

public class Credential
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public byte[]? EncryptedUsername { get; set; }

    public byte[]? UsernameIv { get; set; }

    public byte[]? UsernameTag { get; set; }

    public byte[]? EncryptedPassword { get; set; }

    public byte[]? PasswordIv { get; set; }

    public byte[]? PasswordTag { get; set; }

    public string? ConnectionValue { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public long CreatedByUserId { get; set; }

    public User CreatedByUser { get; set; } = null!;

    public long? UpdatedByUserId { get; set; }

    public User? UpdatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastAccessedAt { get; set; }

    public ICollection<CredentialAccess> AccessRules { get; set; } = new List<CredentialAccess>();

    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}