using System.Text;

namespace LeakGuard.Core.Models;

/// <summary>
/// Результат сканирования — одна найденная запись.
/// </summary>
public class ScanResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Полный путь к файлу</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Категория обнаружения</summary>
    public ScanCategory Category { get; set; }

    /// <summary>Уровень риска</summary>
    public RiskLevel RiskLevel { get; set; }

    /// <summary>Причина обнаружения (кратко)</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Уверенность в обнаружении: 0.0 — 1.0</summary>
    public double Confidence { get; set; }

    /// <summary>Безопасная рекомендация</summary>
    public string Recommendation { get; set; } = string.Empty;

    /// <summary>Замаскированный пример содержимого (до 200 символов)</summary>
    public string? MaskedExample { get; set; }

    /// <summary>Техническая информация (для отчёта специалиста)</summary>
    public string? TechnicalInfo { get; set; }

    /// <summary>Тип найденного файла (для детализации)</summary>
    public string? FileType { get; set; }

    /// <summary>Дата последнего изменения файла</summary>
    public DateTime? FileModifiedDate { get; set; }

    /// <summary>Размер файла в байтах</summary>
    public long? FileSize { get; set; }

    /// <summary>Игнорируется ли этот результат</summary>
    public bool IsIgnored { get; set; }

    /// <summary>Включён ли в белый список</summary>
    public bool IsWhitelisted { get; set; }

    public string RiskLevelDisplay => RiskLevel switch
    {
        RiskLevel.Info => "Информация",
        RiskLevel.Low => "Низкий риск",
        RiskLevel.Medium => "Средний риск",
        RiskLevel.High => "Высокий риск",
        _ => RiskLevel.ToString()
    };

    public string CategoryDisplay => Category switch
    {
        ScanCategory.PersonalData => "Персональные данные",
        ScanCategory.Documents => "Документы",
        ScanCategory.Archives => "Архивы",
        ScanCategory.ImageMetadata => "Метаданные изображений",
        ScanCategory.SensitiveDocuments => "Чувствительные документы",
        ScanCategory.DeveloperFiles => "Файлы разработчика",
        ScanCategory.BackupFiles => "Резервные копии",
        ScanCategory.SecuritySettings => "Настройки безопасности",
        ScanCategory.StartupItems => "Автозапуск",
        ScanCategory.SharePermissions => "Права доступа",
        ScanCategory.BrowserExtensions => "Расширения браузеров",
        _ => Category.ToString()
    };

    public override string ToString()
    {
        return $"[{RiskLevelDisplay}] {CategoryDisplay}: {Reason} — {FilePath}";
    }
}
