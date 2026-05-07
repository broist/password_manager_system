using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PasswordManagerSystem.Api.Application.DTOs.Companies;
using PasswordManagerSystem.Api.Application.Interfaces;
using PasswordManagerSystem.Api.Domain.Entities;
using PasswordManagerSystem.Api.Infrastructure.Data;

namespace PasswordManagerSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CompaniesController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IAuditService _auditService;

    public CompaniesController(
        AppDbContext dbContext,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCompanies()
    {
        var companies = await _dbContext.Companies
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new CompanyResponse
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive,
                CreatedByUserId = x.CreatedByUserId,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return Ok(companies);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetCompanyById(long id)
    {
        var company = await _dbContext.Companies
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new CompanyResponse
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive,
                CreatedByUserId = x.CreatedByUserId,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (company is null)
        {
            return NotFound(new
            {
                message = "Company not found."
            });
        }

        return Ok(company);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Company name is required."
            });
        }

        var normalizedName = request.Name.Trim();

        var exists = await _dbContext.Companies
            .AnyAsync(x => x.Name == normalizedName);

        if (exists)
        {
            return Conflict(new
            {
                message = "Company with this name already exists."
            });
        }

        var currentUserId = GetCurrentUserId();
        var currentAdUsername = GetCurrentAdUsername();

        var company = new Company
        {
            Name = normalizedName,
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Companies.Add(company);
        await _dbContext.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "COMPANY_CREATED",
            success: true,
            userId: currentUserId,
            adUsername: currentAdUsername,
            targetType: "Company",
            targetId: company.Id,
            companyId: company.Id,
            details: $"Company created: {company.Name}"
        );

        var response = new CompanyResponse
        {
            Id = company.Id,
            Name = company.Name,
            Description = company.Description,
            IsActive = company.IsActive,
            CreatedByUserId = company.CreatedByUserId,
            CreatedAt = company.CreatedAt,
            UpdatedAt = company.UpdatedAt
        };

        return CreatedAtAction(
            nameof(GetCompanyById),
            new { id = company.Id },
            response
        );
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateCompany(
        long id,
        [FromBody] UpdateCompanyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Company name is required."
            });
        }

        var company = await _dbContext.Companies
            .FirstOrDefaultAsync(x => x.Id == id);

        if (company is null)
        {
            return NotFound(new
            {
                message = "Company not found."
            });
        }

        var normalizedName = request.Name.Trim();

        var duplicateExists = await _dbContext.Companies
            .AnyAsync(x => x.Id != id && x.Name == normalizedName);

        if (duplicateExists)
        {
            return Conflict(new
            {
                message = "Another company with this name already exists."
            });
        }

        company.Name = normalizedName;
        company.Description = request.Description?.Trim();
        company.IsActive = request.IsActive;
        company.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        var currentUserId = GetCurrentUserId();
        var currentAdUsername = GetCurrentAdUsername();

        await _auditService.LogAsync(
            action: "COMPANY_UPDATED",
            success: true,
            userId: currentUserId,
            adUsername: currentAdUsername,
            targetType: "Company",
            targetId: company.Id,
            companyId: company.Id,
            details: $"Company updated: {company.Name}"
        );

        return Ok(new CompanyResponse
        {
            Id = company.Id,
            Name = company.Name,
            Description = company.Description,
            IsActive = company.IsActive,
            CreatedByUserId = company.CreatedByUserId,
            CreatedAt = company.CreatedAt,
            UpdatedAt = company.UpdatedAt
        });
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteCompany(long id)
    {
        var company = await _dbContext.Companies
            .FirstOrDefaultAsync(x => x.Id == id);

        if (company is null)
        {
            return NotFound(new
            {
                message = "Company not found."
            });
        }

        company.IsActive = false;
        company.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        var currentUserId = GetCurrentUserId();
        var currentAdUsername = GetCurrentAdUsername();

        await _auditService.LogAsync(
            action: "COMPANY_DEACTIVATED",
            success: true,
            userId: currentUserId,
            adUsername: currentAdUsername,
            targetType: "Company",
            targetId: company.Id,
            companyId: company.Id,
            details: $"Company deactivated: {company.Name}"
        );

        return NoContent();
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

    private string GetCurrentAdUsername()
    {
        return User.FindFirstValue("ad_username") ?? "UNKNOWN";
    }
}