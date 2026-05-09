using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PasswordManagerSystem.Client.Services.Auth;

/// <summary>
/// Az aktuális access token + refresh token rekordja.
/// Az AccessTokenExpiresAtUtc segítségével tudja a kliens, mikor érdemes refresh-elni.
/// </summary>
public sealed record TokenBundle(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc);

public interface ITokenStore
{
    /// <summary>Visszaadja az aktuális tokent, vagy null ha nincs / korrupt a tárolt érték.</summary>
    TokenBundle? Load();

    /// <summary>Elmenti a tokent DPAPI-vel titkosítva a felhasználói profilba.</summary>
    void Save(TokenBundle bundle);

    /// <summary>Törli a tárolt tokent (logout).</summary>
    void Clear();
}

/// <summary>
/// Token store, amely DPAPI-vel (CurrentUser scope) titkosítja a tokeneket
/// és a felhasználói AppData mappába menti.
///
/// DPAPI: a Windows beépített titkosítása, csak az aktuális Windows user fiókjából
/// nyitható vissza ugyanazon a gépen. Tehát ha valaki más megszerzi a fájlt,
/// nem tudja visszafejteni.
/// </summary>
public sealed class DpapiTokenStore : ITokenStore
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("PasswordManagerSystem.Client.v1");

    private readonly string _filePath;

    public DpapiTokenStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "PasswordManagerSystem", "Client");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "session.bin");
    }

    public TokenBundle? Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return null;
            }

            var encrypted = File.ReadAllBytes(_filePath);
            var decrypted = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
            var json = Encoding.UTF8.GetString(decrypted);

            return JsonSerializer.Deserialize<TokenBundle>(json);
        }
        catch
        {
            // Ha bármi gond van (korrupt, idegen user fiókkal titkosított), eldobjuk és új loginnal indulunk.
            return null;
        }
    }

    public void Save(TokenBundle bundle)
    {
        var json = JsonSerializer.Serialize(bundle);
        var plain = Encoding.UTF8.GetBytes(json);
        var encrypted = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_filePath, encrypted);
    }

    public void Clear()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }
        catch
        {
            // Best-effort — ha nem sikerül törölni, az nem kritikus.
        }
    }
}
