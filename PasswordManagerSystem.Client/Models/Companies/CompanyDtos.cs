namespace PasswordManagerSystem.Client.Models.Companies;

public sealed class CompanyResponse
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class CreateCompanyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class UpdateCompanyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
