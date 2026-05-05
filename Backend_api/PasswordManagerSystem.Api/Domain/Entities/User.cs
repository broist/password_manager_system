namespace PasswordManagerSystem.Api.Domain.Entities;

public class User
{
    public long Id { get; set; }

    public string AdUsername { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? Email { get; set; }

    public long RoleId { get; set; }

    public Role Role { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTime? FirstLoginAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime? RoleSyncedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Credential> CreatedCredentials { get; set; } = new List<Credential>();

    public ICollection<Credential> UpdatedCredentials { get; set; } = new List<Credential>();

    public ICollection<CredentialAccess> UserAccessRules { get; set; } = new List<CredentialAccess>();

    public ICollection<CredentialAccess> CreatedAccessRules { get; set; } = new List<CredentialAccess>();

    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}