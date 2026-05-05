namespace PasswordManagerSystem.Api.Domain.Entities;

public class Company
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public long? CreatedByUserId { get; set; }

    public User? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Credential> Credentials { get; set; } = new List<Credential>();

    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}