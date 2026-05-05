using PasswordManagerSystem.Api.Application.DTOs;

namespace PasswordManagerSystem.Api.Application.Interfaces;

public interface IAdAuthenticationService
{
    Task<AdUserResult?> AuthenticateAsync(string username, string password);
}