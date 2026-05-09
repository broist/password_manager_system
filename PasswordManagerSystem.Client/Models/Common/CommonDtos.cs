namespace PasswordManagerSystem.Client.Models.Common;

/// <summary>
/// Az API egységes hibaválasza. Pl. { "message": "Invalid username or password." }
/// </summary>
public sealed class ApiErrorResponse
{
    public string? Message { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
}

/// <summary>
/// Health endpoint response.
/// </summary>
public sealed class HealthResponse
{
    public string Status { get; set; } = string.Empty;
    public string Api { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public DateTime UtcTime { get; set; }
}

/// <summary>
/// A backendnél használt belső szerepkör nevek konstansai.
/// Ezek a JWT-ben "role_name" claim-ben szerepelnek és a kliens
/// ezekkel ellenőriz jogosultságokat.
/// </summary>
public static class RoleNames
{
    public const string ItAdmin = "ITAdmin";
    public const string It = "IT";
    public const string Consultant = "Consultant";
    public const string Support = "Support";
}
