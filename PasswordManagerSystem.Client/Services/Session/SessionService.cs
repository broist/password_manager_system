using PasswordManagerSystem.Client.Models.Auth;
using PasswordManagerSystem.Client.Models.Common;

namespace PasswordManagerSystem.Client.Services.Session;

/// <summary>
/// Az aktuálisan bejelentkezett user / szerepkör nyilvántartása.
/// A ViewModelek ehhez fordulnak ha tudni akarják ki a user, mi a szerepköre,
/// és felelős a session-state változások közvetítéséért.
/// </summary>
public interface ISessionService
{
    UserInfo? CurrentUser { get; }
    RoleInfo? CurrentRole { get; }
    IReadOnlyList<string> Groups { get; }

    bool IsAuthenticated { get; }
    bool IsItAdmin { get; }
    bool IsIt { get; }
    bool CanCreateCredentials { get; }
    bool CanManageAccess { get; }

    event EventHandler? SessionChanged;

    void SetSession(LoginResponse loginResponse);
    void ClearSession();
}

public sealed class SessionService : ISessionService
{
    public UserInfo? CurrentUser { get; private set; }
    public RoleInfo? CurrentRole { get; private set; }
    public IReadOnlyList<string> Groups { get; private set; } = Array.Empty<string>();

    public bool IsAuthenticated => CurrentUser is not null;

    public bool IsItAdmin => string.Equals(
        CurrentRole?.Name,
        RoleNames.ItAdmin,
        StringComparison.OrdinalIgnoreCase);

    public bool IsIt => string.Equals(
        CurrentRole?.Name,
        RoleNames.It,
        StringComparison.OrdinalIgnoreCase);

    /// <summary>ITAdmin és IT készíthet új bejegyzést.</summary>
    public bool CanCreateCredentials => IsItAdmin || IsIt;

    /// <summary>Csak ITAdmin kezelhet teljeskörűen ACL szabályokat.</summary>
    public bool CanManageAccess => IsItAdmin;

    public event EventHandler? SessionChanged;

    public void SetSession(LoginResponse loginResponse)
    {
        CurrentUser = loginResponse.User;
        CurrentRole = loginResponse.Role;
        Groups = loginResponse.Groups.AsReadOnly();

        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClearSession()
    {
        CurrentUser = null;
        CurrentRole = null;
        Groups = Array.Empty<string>();

        SessionChanged?.Invoke(this, EventArgs.Empty);
    }
}
