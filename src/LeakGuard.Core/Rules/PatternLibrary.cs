using System.Text.RegularExpressions;

namespace LeakGuard.Core.Rules;

/// <summary>
/// Библиотека regex-паттернов для обнаружения персональных данных.
/// </summary>
public static class PatternLibrary
{
    // --- Контактные данные ---
    public const string Email = @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}";
    public const string PhoneRU = @"(\+7|8)\s*[\(]?\d{3}[\)]?\s*\d{3}[-]?\d{2}[-]?\d{2}";
    public const string PhoneGeneric = @"\+?[1-9]\d{8,14}";
    public const string INN = @"\d{10,12}";
    public const string Snils = @"\d{3}-\d{3}-\d{3}\s?\d{2}";
    public const string PassportRU = @"\d{4}\s?\d{6}";

    // --- Персональные данные ---
    public const string DateOfBirth = @"\d{2}[-./]\d{2}[-./]\d{4}";
    public const string Url = @"https?://[^\s<>""']+[^\s<>""'\.,;:!?]";
    public const string TelegramHandle = @"@[\w]{5,32}";

    // --- Чувствительные слова в именах файлов ---
    public static readonly string[] SensitiveFileNames =
    [
        "паспорт", "inn", "снилс", "инн", "справка", "выписка",
        "password", "secret", "ключ", "token", "private",
        "backup", "export", "database", "dump",
        "credentials", "access_key", "api_key", "config",
        ".env", "htpasswd", ".pem", ".p12", ".pfx"
    ];

    // --- Чувствительные слова в содержимом ---
    public static readonly string[] SensitiveKeywords =
    [
        "пароль", "password", "секрет", "secret", "ключ", "key",
        "токен", "token", "api_key", "api_secret", "access_token",
        "свидетельство", "свидетельство о рождении", "свидетельство о браке",
        "банковские реквизиты", "реквизиты", "расчётный счёт",
        " iban", "bic", "swift",
        "cvv", "cvv2", "security code"
    ];

    // --- Расширения файлов ---
    public static readonly HashSet<string> DocumentExtensions =
    [".docx", ".xlsx", ".pptx", ".pdf", ".txt", ".csv", ".json", ".rtf", ".odt", ".ods"];

    public static readonly HashSet<string> ArchiveExtensions =
    [".zip", ".7z", ".rar", ".tar", ".gz", ".bz2"];

    public static readonly HashSet<string> ImageExtensions =
    [".jpg", ".jpeg", ".png", ".tiff", ".tif", ".bmp", ".heic", ".raw"];

    public static readonly HashSet<string> SensitiveFileExtensions =
    [".env", ".pem", ".p12", ".pfx", ".key", ".ppk", ".putty", ".ppk", "htpasswd"];

    public static readonly HashSet<string> BackupExtensions =
    [".bak", ".backup", ".old", ".orig", ".save", ".db", ".sql", ".sqlite"];

    // --- Regex-компилированные шаблоны ---
    private static readonly Dictionary<string, Regex> _compiledPatterns = new();

    public static Regex GetPattern(string name)
    {
        if (_compiledPatterns.TryGetValue(name, out var regex))
            return regex;

        var pattern = name switch
        {
            nameof(Email) => Email,
            nameof(PhoneRU) => PhoneRU,
            nameof(PhoneGeneric) => PhoneGeneric,
            nameof(INN) => INN,
            nameof(Snils) => Snils,
            nameof(PassportRU) => PassportRU,
            nameof(DateOfBirth) => DateOfBirth,
            nameof(Url) => Url,
            nameof(TelegramHandle) => TelegramHandle,
            _ => string.Empty
        };

        regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        _compiledPatterns[name] = regex;
        return regex;
    }
}
