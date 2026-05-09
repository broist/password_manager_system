namespace PasswordManagerSystem.Client.Models.Auth;

public sealed class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresInMinutes { get; set; }
    public UserInfo User { get; set; } = new();
    public RoleInfo Role { get; set; } = new();
    public List<string> Groups { get; set; } = new();
}

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class RefreshTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresInMinutes { get; set; }
}

public sealed class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class UserInfo
{
    public long Id { get; set; }
    public string AdUsername { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public long RoleId { get; set; }
    public bool IsActive { get; set; }
    public DateTime? FirstLoginAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? RoleSyncedAt { get; set; }
}

public sealed class RoleInfo
{
    public long Id { get; set; }

    /// <summary>
    /// Belső név, pl. "ITAdmin", "IT", "Consultant", "Support".
    /// Erre épül a kliens jogosultságkezelése.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string AdGroupName { get; set; } = string.Empty;

    public int Level { get; set; }
}
