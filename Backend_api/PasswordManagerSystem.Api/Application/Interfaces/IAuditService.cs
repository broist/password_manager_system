namespace PasswordManagerSystem.Api.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        string action,
        bool success,
        long? userId = null,
        string? adUsername = null,
        string? targetType = null,
        long? targetId = null,
        long? credentialId = null,
        long? companyId = null,
        long? targetUserId = null,
        string? details = null
    );
}