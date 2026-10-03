namespace LeakGuard.Core.Models;

/// <summary>
/// Категория обнаружения.
/// </summary>
public enum ScanCategory
{
    /// <summary>Персональные данные: ФИО, email, телефон</summary>
    PersonalData,

    /// <summary>Документы с данными (docx, xlsx, pdf, csv)</summary>
    Documents,

    /// <summary>Архивы, содержащие файлы</summary>
    Archives,

    /// <summary>Метаданные изображений (EXIF, GPS)</summary>
    ImageMetadata,

    /// <summary>Чувствительные документы (паспорт, ИНН, СНИЛС)</summary>
    SensitiveDocuments,

    /// <summary>Файлы разработчика (.env, ключи, конфиги)</summary>
    DeveloperFiles,

    /// <summary>Файлы резервных копий и выгрузок</summary>
    BackupFiles,

    /// <summary>Настройки безопасности</summary>
    SecuritySettings,

    /// <summary>Автозапуск и планировщик</summary>
    StartupItems,

    /// <summary>Общие папки и права доступа</summary>
    SharePermissions,

    /// <summary>Расширения браузеров</summary>
    BrowserExtensions
}
