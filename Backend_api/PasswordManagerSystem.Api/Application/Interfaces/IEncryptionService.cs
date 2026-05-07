using PasswordManagerSystem.Api.Application.DTOs.Security;

namespace PasswordManagerSystem.Api.Application.Interfaces;

public interface IEncryptionService
{
    EncryptedValue Encrypt(string plaintext);

    string Decrypt(EncryptedValue encryptedValue);
}