namespace PasswordManagerSystem.Api.Application.DTOs;

public class AdUserResult
{
    public string AdUsername { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public List<string> Groups { get; set; } = new();
}