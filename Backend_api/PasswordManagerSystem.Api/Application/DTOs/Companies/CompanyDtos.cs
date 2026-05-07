namespace PasswordManagerSystem.Api.Application.DTOs.Companies;

public class CompanyResponse
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public long? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public class CreateCompanyRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public class UpdateCompanyRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}