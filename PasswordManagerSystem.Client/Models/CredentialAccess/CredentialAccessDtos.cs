namespace PasswordManagerSystem.Client.Models.CredentialAccess;

public sealed class CredentialAccessResponse
{
    public long Id { get; set; }
    public long CredentialId { get; set; }
    public long? RoleId { get; set; }
    public string? RoleName { get; set; }
    public long? UserId { get; set; }
    public string? AdUsername { get; set; }
    public bool CanView { get; set; }
    public bool CanWrite { get; set; }
    public bool CanDelete { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class CreateCredentialAccessRequest
{
    public long CredentialId { get; set; }
    public long? RoleId { get; set; }
    public long? UserId { get; set; }
    public bool CanView { get; set; } = true;
    public bool CanWrite { get; set; } = false;
    public bool CanDelete { get; set; } = false;
    public DateTime? ExpiresAt { get; set; }
}
