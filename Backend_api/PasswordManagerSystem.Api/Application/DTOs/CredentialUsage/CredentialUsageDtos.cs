namespace PasswordManagerSystem.Api.Application.DTOs.CredentialUsage;

public sealed class ActiveCredentialUsageResponse
{
    public long Id { get; set; }

    public long CredentialId { get; set; }

    public long UserId { get; set; }

    public string AdUsername { get; set; } = string.Empty;

    public string? ConnectionValue { get; set; }

    public DateTime StartedAt { get; set; }

    public string Status { get; set; } = string.Empty;
}

public sealed class StartCredentialUsageRequest
{
    public long CredentialId { get; set; }

    public string? ConnectionValue { get; set; }

    public int? ProcessId { get; set; }

    public bool OverrideActiveUsage { get; set; }
}

public sealed class StartCredentialUsageResponse
{
    public long Id { get; set; }

    public long CredentialId { get; set; }

    public DateTime StartedAt { get; set; }

    public string Status { get; set; } = string.Empty;
}

public sealed class EndCredentialUsageRequest
{
    public int? ProcessId { get; set; }
}