namespace LeakGuard.Core.Models;

/// <summary>
/// Глобальные настройки приложения.
/// </summary>
public class AppSettings
{
    /// <summary>Язык интерфейса (ru/en)</summary>
    public string Language { get; set; } = "ru";

    /// <summary>Тёмная тема</summary>
    public bool DarkTheme { get; set; } = true;

    /// <summary>Автозапуск после входа в Windows</summary>
    public bool AutoStartEnabled { get; set; }

    /// <summary>По умолчанию отправлять данные в интернет</summary>
    public bool InternetAccessEnabled { get; set; } = false;

    /// <summary>Включать пути файлов в отчёте</summary>
    public bool IncludeFilePathsInReport { get; set; } = true;

    /// <summary>Включать замаскированные примеры в отчёте</summary>
    public bool IncludeMaskedExamplesInReport { get; set; } = true;

    /// <summary>Включать техническую информацию в отчёте</summary>
    public bool IncludeTechnicalInfoInReport { get; set; } = false;

    /// <summary>Шифровать отчёты DPAPI</summary>
    public bool EncryptReports { get; set; } = false;

    /// <summary>Автоудаление отчётов: 0 = никогда, 1, 7, 30 дней</summary>
    public int AutoDeleteReportDays { get; set; } = 0;

    /// <summary>Приоритет сканирования: 0=низкий, 1=нормальный, 2=высокий</summary>
    public int ScanPriority { get; set; } = 1;

    /// <summary>Лимит скорости диска (MB/s), 0 = без лимита</summary>
    public double DiskSpeedLimitMbs { get; set; } = 0;

    /// <summary>Последний выбранный формат отчёта</summary>
    public ReportFormat LastReportFormat { get; set; } = ReportFormat.Html;

    /// <summary>Список игнорируемых результатов (Id)</summary>
    public List<string> IgnoredIds { get; set; } = new();

    /// <summary>Список исключённых папок</summary>
    public List<string> ExcludedFolders { get; set; } = new();

    /// <summary>Список исключённых расширений</summary>
    public List<string> ExcludedExtensions { get; set; } = new();

    /// <summary>Дата последнего сканирования</summary>
    public DateTime? LastScanDate { get; set; }

    /// <summary>Расписание сканирования: еженедельно/ежемесячно/вручную</summary>
    public string ScanSchedule { get; set; } = "manual";

    /// <summary>День недели для еженедельного сканирования (0=Вс)</summary>
    public int WeeklyScanDay { get; set; } = 0;

    /// <summary>Час для автоматического сканирования</summary>
    public int ScanHour { get; set; } = 10;

    /// <summary>Минута для автоматического сканирования</summary>
    public int ScanMinute { get; set; } = 0;
}
