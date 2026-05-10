namespace PasswordManagerSystem.Client.Models.Users;

public sealed class ActiveUserResponse
{
    public long Id { get; set; }

    public string AdUsername { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? Email { get; set; }

    public long RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public string RoleDisplayName { get; set; } = string.Empty;

    public string DisplayText
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(DisplayName))
            {
                return $"{DisplayName} ({AdUsername})";
            }

            return AdUsername;
        }
    }
}