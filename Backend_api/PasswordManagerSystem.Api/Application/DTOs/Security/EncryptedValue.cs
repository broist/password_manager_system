namespace PasswordManagerSystem.Api.Application.DTOs.Security;

public class EncryptedValue
{
    public byte[] CipherText { get; set; } = Array.Empty<byte>();

    public byte[] Iv { get; set; } = Array.Empty<byte>();

    public byte[] Tag { get; set; } = Array.Empty<byte>();
}