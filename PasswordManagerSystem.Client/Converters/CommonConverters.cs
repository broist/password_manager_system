using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PasswordManagerSystem.Client.Converters;

/// <summary>
/// bool -> Visibility (true = Visible, false = Collapsed).
/// Inverse=true esetén fordítva.
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public bool Inverse { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var b = value is bool flag && flag;
        if (Inverse) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : false;
}

/// <summary>
/// String -> bool. True ha a string nem null és nem üres.
/// </summary>
public sealed class StringNotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Bármi -> Visibility. Null = Collapsed, minden más = Visible.
/// </summary>
public sealed class NotNullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// String -> kapcsolódás-típus jelzőszöveg ("URL", "RDP", "—").
/// A "Kapcsolódás" mező értéke alapján döntjük el milyen típusú.
/// </summary>
public sealed class ConnectionTypeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = value as string;
        if (string.IsNullOrWhiteSpace(s)) return "—";

        if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return "URL";
        }

        if (s.StartsWith("rdp://", StringComparison.OrdinalIgnoreCase) ||
            s.StartsWith("\\\\") ||
            s.EndsWith(".rdp", StringComparison.OrdinalIgnoreCase))
        {
            return "RDP";
        }

        return "Egyéb";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// DateTime -> rövid emberi formátum (pl. "2026-05-08 14:23")
/// </summary>
public sealed class ShortDateTimeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dt)
        {
            var local = dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;
            return local.ToString("yyyy-MM-dd HH:mm");
        }

        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Az első karakter (avatar fallback). Pl. "Bíró István" -> "B"
/// </summary>
public sealed class InitialsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = (value as string)?.Trim();
        if (string.IsNullOrEmpty(s)) return "?";

        var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
        }

        return char.ToUpper(parts[0][0]).ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class CredentialTypeDisplayNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Normalize(value as string) switch
        {
            "DATABASE" => "Adatbázis",
            "WINDOWS_SERVER" => "Windows szerver",
            "LINUX_SERVER" => "Linux szerver",
            "VPN" => "VPN kapcsolat",
            "GENERIC" => "Általános",
            _ => "Általános"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static string Normalize(string? credentialType)
    {
        var normalized = credentialType?.Trim().ToUpperInvariant();

        return normalized switch
        {
            "DATABASE" => "DATABASE",
            "WINDOWS_SERVER" => "WINDOWS_SERVER",
            "LINUX_SERVER" => "LINUX_SERVER",
            "VPN" => "VPN",
            "GENERIC" => "GENERIC",
            _ => "GENERIC"
        };
    }
}

public sealed class CredentialTypeShortNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Normalize(value as string) switch
        {
            "DATABASE" => "DB",
            "WINDOWS_SERVER" => "WIN",
            "LINUX_SERVER" => "LNX",
            "VPN" => "VPN",
            "GENERIC" => "ALT",
            _ => "ALT"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static string Normalize(string? credentialType)
    {
        var normalized = credentialType?.Trim().ToUpperInvariant();

        return normalized switch
        {
            "DATABASE" => "DATABASE",
            "WINDOWS_SERVER" => "WINDOWS_SERVER",
            "LINUX_SERVER" => "LINUX_SERVER",
            "VPN" => "VPN",
            "GENERIC" => "GENERIC",
            _ => "GENERIC"
        };
    }
}