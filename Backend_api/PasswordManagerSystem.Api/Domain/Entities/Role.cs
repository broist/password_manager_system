namespace PasswordManagerSystem.Api.Domain.Entities;

public class Role
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string AdGroupName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int Level { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<User> Users { get; set; } = new List<User>();

    public ICollection<CredentialAccess> CredentialAccessRules { get; set; } = new List<CredentialAccess>();
}