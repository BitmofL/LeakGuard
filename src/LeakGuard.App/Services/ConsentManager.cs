using System.Windows;
using LeakGuard.Core.Models;

namespace LeakGuard.App.Services;

/// <summary>
/// Результат запроса согласия.
/// </summary>
public enum ConsentResult
{
    Granted,
    Denied,
    Cancelled
}

/// <summary>
/// Менеджер управления согласием пользователя.
/// Обеспечивает явное подтверждение перед операциями с повышенными правами.
/// </summary>
public class ConsentManager
{
    /// <summary>
    /// Запрашивает согласие у пользователя через диалоговое окно.
    /// </summary>
    /// <param name="title">Заголовок окна</param>
    /// <param name="message">Основное сообщение</param>
    /// <param name="detail">Детальное описание (что именно будет сделано)</param>
    /// <param name="requiresUac">Требует ли операция повышения прав (UAC)</param>
    /// <returns>Результат согласия</returns>
    public ConsentResult RequestConsent(
        string title,
        string message,
        string? detail = null,
        bool requiresUac = false)
    {
        var icon = requiresUac ? MessageBoxImage.Warning : MessageBoxImage.Information;
        var buttons = requiresUac
            ? MessageBoxButton.YesNo
            : MessageBoxButton.YesNoCancel;

        var detailText = !string.IsNullOrEmpty(detail)
            ? $"\n\n{detail}"
            : string.Empty;

        var uacNote = requiresUac
            ? "\n\nБудет запрошено стандартное окно подтверждения Windows (UAC)."
            : string.Empty;

        var result = MessageBox.Show(
            $"{message}{detailText}{uacNote}",
            title,
            buttons,
            icon);

        return result switch
        {
            MessageBoxResult.Yes => ConsentResult.Granted,
            MessageBoxResult.No => ConsentResult.Denied,
            _ => ConsentResult.Cancelled
        };
    }

    /// <summary>
    /// Запрашивает согласие на сканирование системных папок.
    /// </summary>
    public ConsentResult RequestSystemScanConsent()
    {
        return RequestConsent(
            "LeakGuard — Сканирование системных папок",
            "Для проверки автозапуска всех пользователей и системных настроек безопасности потребуются повышенные права.",
            "Будет проверен автозапуск всех пользователей, системные задачи планировщика и параметры безопасности. Это стандартная операция Windows, требующая подтверждения UAC.",
            requiresUac: true);
    }

    /// <summary>
    /// Запрашивает согласие на полное сканирование.
    /// </summary>
    public ConsentRequest FullScanConsent(
        IEnumerable<string> paths,
        int estimatedFileCount)
    {
        var pathList = paths.ToList();
        var pathSummary = pathList.Count > 3
            ? $"{pathList.Count} дисков/папок (показаны первые 3): {string.Join(", ", pathList.Take(3))}..."
            : string.Join(", ", pathList);

        return new ConsentRequest(
            "LeakGuard — Полное сканирование",
            $"Полное сканирование начнёт проверку {estimatedFileCount:N0} файлов в следующих областях:\n{pathSummary}",
            "Это может занять от нескольких минут до часов в зависимости от объёма данных. Нагрузка на диск будет минимальной. Данные не отправляются в интернет.");
    }

    /// <summary>
    /// Запрашивает согласие на интернет-проверку.
    /// </summary>
    public ConsentResult RequestInternetConsent(string emailOrIdentifier)
    {
        return RequestConsent(
            "LeakGuard — Интернет-проверка",
            "Для проверки утечек потребуется отправить хеш email в публичный API. Сам email не будет сохранён.",
            $"Будет отправлен SHA-256 хеш значения: {emailOrIdentifier}. Сам email не сохраняется и не передаётся в полном виде. Результат показывается без сохранения.",
            requiresUac: false);
    }

    /// <summary>
    /// Запрашивает согласие на экспорт отчёта.
    /// </summary>
    public ConsentResult RequestExportConsent(string format, string path)
    {
        return RequestConsent(
            "LeakGuard — Экспорт отчёта",
            $"Отчёт будет сохранён в формате {format} по адресу:\n{path}",
            "Отчёт содержит результаты сканирования. Включены настройки маскирования из ваших параметров.",
            requiresUac: false);
    }

    /// <summary>
    /// Запрашивает согласие на включение автозапуска.
    /// </summary>
    public ConsentResult RequestAutoStartConsent()
    {
        return RequestConsent(
            "LeakGuard — Автозапуск",
            "Приложение будет запускаться при входе в Windows.",
            "Будет создана задача в планировщике задач Windows. Запускается лёгкий уведомляющий модуль без сканирования диска. Вы можете отключить автозапуск в настройках или удалить задачу вручную.",
            requiresUac: false);
    }
}

/// <summary>
/// Запрос согласия с информацией для отображения.
/// </summary>
public class ConsentRequest
{
    public string Title { get; }
    public string Message { get; }
    public string Detail { get; }

    public ConsentRequest(string title, string message, string detail)
    {
        Title = title;
        Message = message;
        Detail = detail;
    }

    public ConsentResult Show()
    {
        var mgr = new ConsentManager();
        return mgr.RequestConsent(Title, Message, Detail);
    }
}
