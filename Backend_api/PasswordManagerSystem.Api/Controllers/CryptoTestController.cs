using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PasswordManagerSystem.Api.Application.Interfaces;

namespace PasswordManagerSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CryptoTestController : ControllerBase
{
    private readonly IEncryptionService _encryptionService;

    public CryptoTestController(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    [HttpPost("roundtrip")]
    public IActionResult Roundtrip([FromBody] CryptoTestRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Plaintext))
        {
            return BadRequest(new
            {
                message = "Plaintext is required."
            });
        }

        var encrypted = _encryptionService.Encrypt(request.Plaintext);
        var decrypted = _encryptionService.Decrypt(encrypted);

        return Ok(new
        {
            encrypted = new
            {
                cipherTextBase64 = Convert.ToBase64String(encrypted.CipherText),
                ivBase64 = Convert.ToBase64String(encrypted.Iv),
                tagBase64 = Convert.ToBase64String(encrypted.Tag)
            },
            decrypted,
            isMatch = decrypted == request.Plaintext
        });
    }
}

public class CryptoTestRequest
{
    public string Plaintext { get; set; } = string.Empty;
}