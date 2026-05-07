using System.Security.Cryptography;
using System.Text;
using PasswordManagerSystem.Api.Application.DTOs.Security;
using PasswordManagerSystem.Api.Application.Interfaces;

namespace PasswordManagerSystem.Api.Infrastructure.Security;

public class AesGcmEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public AesGcmEncryptionService(IConfiguration configuration)
    {
        var base64Key = configuration["Encryption:MasterKey"];

        if (string.IsNullOrWhiteSpace(base64Key))
        {
            throw new InvalidOperationException("Encryption master key is not configured.");
        }

        _key = Convert.FromBase64String(base64Key);

        if (_key.Length != 32)
        {
            throw new InvalidOperationException("Encryption master key must be 32 bytes for AES-256.");
        }
    }

    public EncryptedValue Encrypt(string plaintext)
    {
        if (plaintext is null)
        {
            throw new ArgumentNullException(nameof(plaintext));
        }

        var iv = RandomNumberGenerator.GetBytes(12);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherText = new byte[plaintextBytes.Length];
        var tag = new byte[16];

        using var aesGcm = new AesGcm(_key, 16);

        aesGcm.Encrypt(
            iv,
            plaintextBytes,
            cipherText,
            tag
        );

        CryptographicOperations.ZeroMemory(plaintextBytes);

        return new EncryptedValue
        {
            CipherText = cipherText,
            Iv = iv,
            Tag = tag
        };
    }

    public string Decrypt(EncryptedValue encryptedValue)
    {
        if (encryptedValue.CipherText.Length == 0)
        {
            return string.Empty;
        }

        var plaintextBytes = new byte[encryptedValue.CipherText.Length];

        using var aesGcm = new AesGcm(_key, 16);

        aesGcm.Decrypt(
            encryptedValue.Iv,
            encryptedValue.CipherText,
            encryptedValue.Tag,
            plaintextBytes
        );

        var plaintext = Encoding.UTF8.GetString(plaintextBytes);

        CryptographicOperations.ZeroMemory(plaintextBytes);

        return plaintext;
    }
}