using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PasswordManagerSystem.Api.Application.DTOs.Credentials;
using PasswordManagerSystem.Api.Application.Interfaces;
using PasswordManagerSystem.Api.Domain.Entities;
using PasswordManagerSystem.Api.Infrastructure.Data;

namespace PasswordManagerSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CredentialsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IEncryptionService _encryptionService;
    private readonly IAuditService _auditService;
    private readonly ICredentialAccessService _credentialAccessService;

    public CredentialsController(
        AppDbContext dbContext,
        IEncryptionService encryptionService,
        IAuditService auditService,
        ICredentialAccessService credentialAccessService)
    {
        _dbContext = dbContext;
        _encryptionService = encryptionService;
        _auditService = auditService;
        _credentialAccessService = credentialAccessService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCredentials([FromQuery] long? companyId = null)
    {
        var currentUserId = GetCurrentUserId();
        var currentRoleId = GetCurrentRoleId();

        var query = _dbContext.Credentials
            .AsNoTracking()
            .Include(x => x.Company)
            .Where(x => x.IsActive);

        if (companyId.HasValue)
        {
            query = query.Where(x => x.CompanyId == companyId.Value);
        }

        var allCredentials = await query
            .OrderBy(x => x.Company.Name)
            .ThenBy(x => x.Title)
            .ToListAsync();

        var visibleCredentials = new List<CredentialListItemResponse>();

        foreach (var credential in allCredentials)
        {
            var canView = await _credentialAccessService.CanViewCredentialAsync(
                credential.Id,
                currentUserId,
                currentRoleId
            );

            if (!canView)
            {
                continue;
            }

            visibleCredentials.Add(new CredentialListItemResponse
            {
                Id = credential.Id,
                CompanyId = credential.CompanyId,
                CompanyName = credential.Company.Name,
                Title = credential.Title,
                ConnectionValue = credential.ConnectionValue,
                Notes = credential.Notes,
                IsActive = credential.IsActive,
                CreatedByUserId = credential.CreatedByUserId,
                UpdatedByUserId = credential.UpdatedByUserId,
                CreatedAt = credential.CreatedAt,
                UpdatedAt = credential.UpdatedAt,
                LastAccessedAt = credential.LastAccessedAt
            });
        }

        return Ok(visibleCredentials);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCredential([FromBody] CreateCredentialRequest request)
    {
        if (!IsCurrentUserItAdmin() && !IsCurrentUserIt())
        {
            return Forbid();
        }

        if (request.CompanyId <= 0)
        {
            return BadRequest(new
            {
                message = "CompanyId is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new
            {
                message = "Credential title is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(new
            {
                message = "Username is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new
            {
                message = "Password is required."
            });
        }

        var company = await _dbContext.Companies
            .FirstOrDefaultAsync(x => x.Id == request.CompanyId && x.IsActive);

        if (company is null)
        {
            return NotFound(new
            {
                message = "Active company not found."
            });
        }

        var currentUserId = GetCurrentUserId();
        var currentRoleId = GetCurrentRoleId();
        var currentAdUsername = GetCurrentAdUsername();

        var encryptedUsername = _encryptionService.Encrypt(request.Username);
        var encryptedPassword = _encryptionService.Encrypt(request.Password);

        var now = DateTime.UtcNow;

        var credential = new Credential
        {
            CompanyId = company.Id,
            Title = request.Title.Trim(),

            EncryptedUsername = encryptedUsername.CipherText,
            UsernameIv = encryptedUsername.Iv,
            UsernameTag = encryptedUsername.Tag,

            EncryptedPassword = encryptedPassword.CipherText,
            PasswordIv = encryptedPassword.Iv,
            PasswordTag = encryptedPassword.Tag,

            ConnectionValue = request.ConnectionValue?.Trim(),
            Notes = request.Notes?.Trim(),

            IsActive = true,
            CreatedByUserId = currentUserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Credentials.Add(credential);
        await _dbContext.SaveChangesAsync();

        var accessRule = new CredentialAccess
        {
            CredentialId = credential.Id,
            RoleId = currentRoleId,
            UserId = null,
            CanView = true,
            CanWrite = true,
            CanDelete = true,
            ExpiresAt = null,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.CredentialAccesses.Add(accessRule);
        await _dbContext.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "CREDENTIAL_ACCESS_GRANTED",
            success: true,
            userId: currentUserId,
            adUsername: currentAdUsername,
            targetType: "CredentialAccess",
            targetId: accessRule.Id,
            credentialId: credential.Id,
            companyId: company.Id,
            details: $"Default access granted for role_id: {currentRoleId}, credential: {credential.Title}"
        );

        await _auditService.LogAsync(
            action: "CREDENTIAL_CREATED",
            success: true,
            userId: currentUserId,
            adUsername: currentAdUsername,
            targetType: "Credential",
            targetId: credential.Id,
            credentialId: credential.Id,
            companyId: company.Id,
            details: $"Credential created: {credential.Title}"
        );

        var response = new CredentialDetailResponse
        {
            Id = credential.Id,
            CompanyId = credential.CompanyId,
            CompanyName = company.Name,
            Title = credential.Title,
            ConnectionValue = credential.ConnectionValue,
            Notes = credential.Notes,
            IsActive = credential.IsActive,
            CreatedByUserId = credential.CreatedByUserId,
            UpdatedByUserId = credential.UpdatedByUserId,
            CreatedAt = credential.CreatedAt,
            UpdatedAt = credential.UpdatedAt,
            LastAccessedAt = credential.LastAccessedAt
        };

        return CreatedAtAction(
            nameof(GetCredentialById),
            new { id = credential.Id },
            response
        );
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateCredential(
        long id,
        [FromBody] UpdateCredentialRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new
            {
                message = "Credential title is required."
            });
        }

        var credential = await _dbContext.Credentials
            .Include(x => x.Company)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (credential is null)
        {
            return NotFound(new
            {
                message = "Credential not found."
            });
        }

        if (!credential.Company.IsActive)
        {
            return BadRequest(new
            {
                message = "Credential belongs to an inactive company."
            });
        }

        var currentUserId = GetCurrentUserId();
        var currentRoleId = GetCurrentRoleId();
        var currentAdUsername = GetCurrentAdUsername();

        var canWrite = await _credentialAccessService.CanWriteCredentialAsync(
            id,
            currentUserId,
            currentRoleId
        );

        if (!canWrite)
        {
            return Forbid();
        }

        credential.Title = request.Title.Trim();
        credential.ConnectionValue = request.ConnectionValue?.Trim();
        credential.Notes = request.Notes?.Trim();
        credential.IsActive = request.IsActive;
        credential.UpdatedByUserId = currentUserId;
        credential.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var encryptedUsername = _encryptionService.Encrypt(request.Username);

            credential.EncryptedUsername = encryptedUsername.CipherText;
            credential.UsernameIv = encryptedUsername.Iv;
            credential.UsernameTag = encryptedUsername.Tag;
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var encryptedPassword = _encryptionService.Encrypt(request.Password);

            credential.EncryptedPassword = encryptedPassword.CipherText;
            credential.PasswordIv = encryptedPassword.Iv;
            credential.PasswordTag = encryptedPassword.Tag;
        }

        await _dbContext.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "CREDENTIAL_UPDATED",
            success: true,
            userId: currentUserId,
            adUsername: currentAdUsername,
            targetType: "Credential",
            targetId: credential.Id,
            credentialId: credential.Id,
            companyId: credential.CompanyId,
            details: $"Credential updated: {credential.Title}"
        );

        return Ok(new CredentialDetailResponse
        {
            Id = credential.Id,
            CompanyId = credential.CompanyId,
            CompanyName = credential.Company.Name,
            Title = credential.Title,
            ConnectionValue = credential.ConnectionValue,
            Notes = credential.Notes,
            IsActive = credential.IsActive,
            CreatedByUserId = credential.CreatedByUserId,
            UpdatedByUserId = credential.UpdatedByUserId,
            CreatedAt = credential.CreatedAt,
            UpdatedAt = credential.UpdatedAt,
            LastAccessedAt = credential.LastAccessedAt
        });
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteCredential(long id)
    {
        var credential = await _dbContext.Credentials
            .Include(x => x.Company)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (credential is null)
        {
            return NotFound(new
            {
                message = "Credential not found."
            });
        }

        var currentUserId = GetCurrentUserId();
        var currentRoleId = GetCurrentRoleId();
        var currentAdUsername = GetCurrentAdUsername();

        var canDelete = await _credentialAccessService.CanDeleteCredentialAsync(
            id,
            currentUserId,
            currentRoleId
        );

        if (!canDelete)
        {
            return Forbid();
        }

        credential.IsActive = false;
        credential.UpdatedByUserId = currentUserId;
        credential.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "CREDENTIAL_DEACTIVATED",
            success: true,
            userId: currentUserId,
            adUsername: currentAdUsername,
            targetType: "Credential",
            targetId: credential.Id,
            credentialId: credential.Id,
            companyId: credential.CompanyId,
            details: $"Credential deactivated: {credential.Title}"
        );

        return NoContent();
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetCredentialById(long id)
    {
        var currentUserId = GetCurrentUserId();
        var currentRoleId = GetCurrentRoleId();

        var canView = await _credentialAccessService.CanViewCredentialAsync(
            id,
            currentUserId,
            currentRoleId
        );

        if (!canView)
        {
            return Forbid();
        }

        var credential = await _dbContext.Credentials
            .AsNoTracking()
            .Include(x => x.Company)
            .Where(x => x.Id == id && x.IsActive)
            .Select(x => new CredentialDetailResponse
            {
                Id = x.Id,
                CompanyId = x.CompanyId,
                CompanyName = x.Company.Name,
                Title = x.Title,
                ConnectionValue = x.ConnectionValue,
                Notes = x.Notes,
                IsActive = x.IsActive,
                CreatedByUserId = x.CreatedByUserId,
                UpdatedByUserId = x.UpdatedByUserId,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                LastAccessedAt = x.LastAccessedAt
            })
            .FirstOrDefaultAsync();

        if (credential is null)
        {
            return NotFound(new
            {
                message = "Credential not found."
            });
        }

        return Ok(credential);
    }

    [HttpPost("{id:long}/reveal-username")]
    public async Task<IActionResult> RevealUsername(long id)
    {
        var currentUserId = GetCurrentUserId();
        var currentRoleId = GetCurrentRoleId();
        var currentAdUsername = GetCurrentAdUsername();

        var canView = await _credentialAccessService.CanViewCredentialAsync(
            id,
            currentUserId,
            currentRoleId
        );

        if (!canView)
        {
            return Forbid();
        }

        var credential = await _dbContext.Credentials
            .Include(x => x.Company)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

        if (credential is null)
        {
            return NotFound(new
            {
                message = "Credential not found."
            });
        }

        if (credential.EncryptedUsername is null ||
            credential.UsernameIv is null ||
            credential.UsernameTag is null)
        {
            return BadRequest(new
            {
                message = "Encrypted username is missing."
            });
        }

        var encryptedValue = new PasswordManagerSystem.Api.Application.DTOs.Security.EncryptedValue
        {
            CipherText = credential.EncryptedUsername,
            Iv = credential.UsernameIv,
            Tag = credential.UsernameTag
        };

        var username = _encryptionService.Decrypt(encryptedValue);

        credential.LastAccessedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "CREDENTIAL_USERNAME_REVEALED",
            success: true,
            userId: currentUserId,
            adUsername: currentAdUsername,
            targetType: "Credential",
            targetId: credential.Id,
            credentialId: credential.Id,
            companyId: credential.CompanyId,
            details: $"Username revealed for credential: {credential.Title}"
        );

        return Ok(new
        {
            credentialId = credential.Id,
            title = credential.Title,
            username
        });
    }

    [HttpPost("{id:long}/reveal-password")]
    public async Task<IActionResult> RevealPassword(long id)
    {
        var currentUserId = GetCurrentUserId();
        var currentRoleId = GetCurrentRoleId();
        var currentAdUsername = GetCurrentAdUsername();

        var canView = await _credentialAccessService.CanViewCredentialAsync(
            id,
            currentUserId,
            currentRoleId
        );

        if (!canView)
        {
            return Forbid();
        }

        var credential = await _dbContext.Credentials
            .Include(x => x.Company)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

        if (credential is null)
        {
            return NotFound(new
            {
                message = "Credential not found."
            });
        }

        if (credential.EncryptedPassword is null ||
            credential.PasswordIv is null ||
            credential.PasswordTag is null)
        {
            return BadRequest(new
            {
                message = "Encrypted password is missing."
            });
        }

        var encryptedValue = new PasswordManagerSystem.Api.Application.DTOs.Security.EncryptedValue
        {
            CipherText = credential.EncryptedPassword,
            Iv = credential.PasswordIv,
            Tag = credential.PasswordTag
        };

        var password = _encryptionService.Decrypt(encryptedValue);

        credential.LastAccessedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "CREDENTIAL_PASSWORD_REVEALED",
            success: true,
            userId: currentUserId,
            adUsername: currentAdUsername,
            targetType: "Credential",
            targetId: credential.Id,
            credentialId: credential.Id,
            companyId: credential.CompanyId,
            details: $"Password revealed for credential: {credential.Title}"
        );

        return Ok(new
        {
            credentialId = credential.Id,
            title = credential.Title,
            password
        });
    }

    private long GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue("user_id");

        if (!long.TryParse(userIdValue, out var userId))
        {
            throw new InvalidOperationException("Authenticated user_id claim is missing or invalid.");
        }

        return userId;
    }

    private long GetCurrentRoleId()
    {
        var roleIdValue = User.FindFirstValue("role_id");

        if (!long.TryParse(roleIdValue, out var roleId))
        {
            throw new InvalidOperationException("Authenticated role_id claim is missing or invalid.");
        }

        return roleId;
    }

    private string GetCurrentAdUsername()
    {
        return User.FindFirstValue("ad_username") ?? "UNKNOWN";
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

    private bool IsCurrentUserIt()
    {
        return string.Equals(
            GetCurrentRoleName(),
            "IT",
            StringComparison.OrdinalIgnoreCase
        );
    }
}