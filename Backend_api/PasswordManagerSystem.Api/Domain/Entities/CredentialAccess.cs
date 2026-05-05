namespace PasswordManagerSystem.Api.Domain.Entities;

public class CredentialAccess
{
    public long Id { get; set; }

    public long CredentialId { get; set; }

    public Credential Credential { get; set; } = null!;

    public long? RoleId { get; set; }

    public Role? Role { get; set; }

    public long? UserId { get; set; }

    public User? User { get; set; }

    public bool CanView { get; set; } = true;

    public bool CanWrite { get; set; } = false;

    public bool CanDelete { get; set; } = false;

    public DateTime? ExpiresAt { get; set; }

    public long? CreatedByUserId { get; set; }

    public User? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}