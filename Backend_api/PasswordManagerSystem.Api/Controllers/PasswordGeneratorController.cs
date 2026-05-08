using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PasswordManagerSystem.Api.Application.DTOs.PasswordGenerator;
using PasswordManagerSystem.Api.Application.Interfaces;

namespace PasswordManagerSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class PasswordGeneratorController : ControllerBase
{
    private readonly IPasswordGeneratorService _passwordGeneratorService;

    public PasswordGeneratorController(IPasswordGeneratorService passwordGeneratorService)
    {
        _passwordGeneratorService = passwordGeneratorService;
    }

    [HttpPost("generate")]
    public IActionResult Generate([FromBody] GeneratePasswordRequest request)
    {
        var response = _passwordGeneratorService.Generate(request);

        return Ok(response);
    }
}