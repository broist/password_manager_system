namespace PasswordManagerSystem.Client.Models.PasswordGenerator;

public sealed class GeneratePasswordRequest
{
    public int Length { get; set; } = 20;
    public bool IncludeUppercase { get; set; } = true;
    public bool IncludeLowercase { get; set; } = true;
    public bool IncludeDigits { get; set; } = true;
    public bool IncludeSpecialCharacters { get; set; } = true;
}

public sealed class GeneratePasswordResponse
{
    public string Password { get; set; } = string.Empty;
    public int Length { get; set; }
    public bool IncludesUppercase { get; set; }
    public bool IncludesLowercase { get; set; }
    public bool IncludesDigits { get; set; }
    public bool IncludesSpecialCharacters { get; set; }
}
