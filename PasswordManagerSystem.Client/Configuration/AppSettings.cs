namespace PasswordManagerSystem.Client.Configuration;

/// <summary>
/// Az alkalmazás teljes konfigurációja, appsettings.json-ből töltődik.
/// </summary>
public sealed class AppSettings
{
    public ApiSettings Api { get; set; } = new();
    public SessionSettings Session { get; set; } = new();
}

public sealed class ApiSettings
{
    /// <summary>
    /// A backend Web API alap URL-je. Záró perjellel.
    /// Pl. https://localhost:7050/
    /// </summary>
    public string BaseUrl { get; set; } = "https://localhost:7050/";

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Fejlesztés alatt a self-signed dev tanúsítványt elfogadjuk.
    /// Élesben FALSE!
    /// </summary>
    public bool AcceptInvalidCertificates { get; set; } = false;
}

public sealed class SessionSettings
{
    /// <summary>
    /// Hány másodperccel a token lejárta előtt kérjünk auto-refresh-t.
    /// </summary>
    public int AutoRefreshLeadTimeSeconds { get; set; } = 60;

    /// <summary>
    /// Hány másodperc után tisztítsuk a vágólapot, miután felmásoltunk valamit.
    /// </summary>
    public int ClipboardClearSeconds { get; set; } = 12;

    /// <summary>
    /// Hány másodperc után rejtsük el automatikusan a feltárt jelszót.
    /// </summary>
    public int PasswordRevealAutoMaskSeconds { get; set; } = 30;
}
