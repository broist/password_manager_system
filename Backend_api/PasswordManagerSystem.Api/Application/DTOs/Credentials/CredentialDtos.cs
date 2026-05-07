namespace PasswordManagerSystem.Api.Application.DTOs.Credentials;

public class CredentialListItemResponse
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? ConnectionValue { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public long CreatedByUserId { get; set; }

    public long? UpdatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? LastAccessedAt { get; set; }
}

public class CredentialDetailResponse
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? ConnectionValue { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public long CreatedByUserId { get; set; }

    public long? UpdatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? LastAccessedAt { get; set; }
}

public class CreateCredentialRequest
{
    public long CompanyId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string? ConnectionValue { get; set; }

    public string? Notes { get; set; }
}

public class UpdateCredentialRequest
{
    public string Title { get; set; } = string.Empty;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? ConnectionValue { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
}