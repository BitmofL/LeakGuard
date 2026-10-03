using System.Text.RegularExpressions;

namespace LeakGuard.Core.Services;

/// <summary>
/// Сервис маскирования персональных данных.
/// Используется для безопасного отображения и экспорта.
/// </summary>
public static class DataMasker
{
    /// <summary>
    /// Маскирует email: ivan@example.com -> i***@example.com
    /// </summary>
    public static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1) return email;
        var local = email[..at];
        return local[0] + new string('*', local.Length - 1) + email[at..];
    }

    /// <summary>
    /// Маскирует телефон: +7(999)123-45-67 -> +7(***)***-**-67
    /// </summary>
    public static string MaskPhone(string phone)
    {
        var digits = Regex.Replace(phone, @"\D", "");
        if (digits.Length < 10) return phone;

        var visible = digits[^4..];
        var masked = new string('*', digits.Length - 4);
        return phone[..(phone.Length - visible.Length)] + masked + visible;
    }

    /// <summary>
    /// Маскирует номер документа: 1234 567890 -> **** ****7890
    /// </summary>
    public static string MaskDocumentNumber(string number)
    {
        var digits = Regex.Replace(number, @"\D", "");
        if (digits.Length < 4) return number;

        var visible = digits[^4..];
        var masked = new string('*', digits.Length - 4);
        return masked + visible;
    }

    /// <summary>
    /// Маскирует ключ/токен: abcdef1234567890 -> abc****************890
    /// </summary>
    public static string MaskSecret(string secret)
    {
        if (string.IsNullOrEmpty(secret)) return string.Empty;
        if (secret.Length <= 8) return new string('*', secret.Length);

        var prefix = secret[..3];
        var suffix = secret[^3..];
        return prefix + new string('*', secret.Length - 6) + suffix;
    }

    /// <summary>
    /// Маскирует URL, скрывая параметры запроса.
    /// </summary>
    public static string MaskUrl(string url)
    {
        var question = url.IndexOf('?');
        if (question <= 0) return url;
        return url[..question] + "?" + new string('*', 10);
    }

    /// <summary>
    /// Маскирует путь файла, скрывая чувствительные папки.
    /// </summary>
    public static string MaskPath(string path)
    {
        var sensitiveFolders = new[] { "AppData", ".ssh", ".gnupg", "secrets", "credentials" };
        var masked = path;

        foreach (var folder in sensitiveFolders)
        {
            // Заменяем /папка/ или \папка\ на /***/
            var pattern = @"[\\/]" + Regex.Escape(folder) + @"[\\/]";
            masked = Regex.Replace(masked, pattern, "/***/", RegexOptions.IgnoreCase);
            
            // Если папка в конце пути: \папка
            var patternEnd = @"[\\/]" + Regex.Escape(folder) + @"$";
            masked = Regex.Replace(masked, patternEnd, "/***", RegexOptions.IgnoreCase);
        }

        return masked;
    }

    /// <summary>
    /// Маскирует строку содержимого файла, заменяя ПДН.
    /// </summary>
    public static string MaskContent(string content, int maxLength = 200)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;

        var text = content.Length > maxLength ? content[..maxLength] : content;

        // Маскируем email
        text = Regex.Replace(text,
            @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}",
            m => MaskEmail(m.Value));

        // Маскируем телефоны
        text = Regex.Replace(text,
            @"\+?[\d\s\-\(\)]{10,}",
            m => MaskPhone(m.Value));

        // Маскируем ключи/токены
        text = Regex.Replace(text,
            @"(?i)(api[_\-]?key|token|secret|password|pass)\s*[:=]\s*['""]?([^\s'""]+)['""]?",
            m => m.Groups[1].Value + " = " + MaskSecret(m.Groups[2].Value));

        return text;
    }

    /// <summary>
    /// Создаёт хеш-отпечаток строки (без раскрытия содержимого).
    /// </summary>
    public static string GetFingerprint(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}
