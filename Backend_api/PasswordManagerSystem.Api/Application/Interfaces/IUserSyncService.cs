using PasswordManagerSystem.Api.Application.DTOs;
using PasswordManagerSystem.Api.Domain.Entities;

namespace PasswordManagerSystem.Api.Application.Interfaces;

public interface IUserSyncService
{
    Task<User> SyncUserAsync(AdUserResult adUser, Role resolvedRole);
}