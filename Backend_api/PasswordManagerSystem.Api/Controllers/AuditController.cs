using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PasswordManagerSystem.Api.Application.Interfaces;

namespace PasswordManagerSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly IAuditChainVerifierService _auditChainVerifierService;

    public AuditController(IAuditChainVerifierService auditChainVerifierService)
    {
        _auditChainVerifierService = auditChainVerifierService;
    }

    [HttpGet("verify-chain")]
    public async Task<IActionResult> VerifyChain()
    {
        if (!IsCurrentUserItAdmin())
        {
            return Forbid();
        }

        var result = await _auditChainVerifierService.VerifyAsync();

        return Ok(result);
    }

    private string GetCurrentRoleName()
    {
        return User.FindFirstValue("role_name") ?? string.Empty;
    }

    private bool IsCurrentUserItAdmin()
    {
        return string.Equals(
            GetCurrentRoleName(),
            "ITAdmin",
            StringComparison.OrdinalIgnoreCase
        );
    }
}