namespace PasswordManagerSystem.Api.Domain.Entities;

public class CredentialUsageSession
{
    public long Id { get; set; }

    public long CredentialId { get; set; }

    public Credential Credential { get; set; } = null!;

    public long UserId { get; set; }

    public User User { get; set; } = null!;

    public string AdUsername { get; set; } = string.Empty;

    public string? ConnectionValue { get; set; }

    public int? ProcessId { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? EndedAt { get; set; }

    public string Status { get; set; } = "ACTIVE";
}