namespace PasswordManagerSystem.Api.Application.DTOs.Auth;

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public string TokenType { get; set; } = "Bearer";

    public int ExpiresInMinutes { get; set; }

    public UserInfoResponse User { get; set; } = new();

    public RoleInfoResponse Role { get; set; } = new();

    public List<string> Groups { get; set; } = new();
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class RefreshTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public string TokenType { get; set; } = "Bearer";

    public int ExpiresInMinutes { get; set; }
}

public class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class UserInfoResponse
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

public class RoleInfoResponse
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string AdGroupName { get; set; } = string.Empty;

    public int Level { get; set; }
}