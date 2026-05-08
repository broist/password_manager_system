using PasswordManagerSystem.Api.Application.DTOs.Audit;

namespace PasswordManagerSystem.Api.Application.Interfaces;

public interface IAuditChainVerifierService
{
    Task<AuditChainVerificationResponse> VerifyAsync();
}