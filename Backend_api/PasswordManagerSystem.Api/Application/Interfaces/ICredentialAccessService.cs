namespace PasswordManagerSystem.Api.Application.Interfaces;

public interface ICredentialAccessService
{
    Task<bool> CanViewCredentialAsync(long credentialId, long userId, long roleId);

    Task<bool> CanWriteCredentialAsync(long credentialId, long userId, long roleId);

    Task<bool> CanDeleteCredentialAsync(long credentialId, long userId, long roleId);
}