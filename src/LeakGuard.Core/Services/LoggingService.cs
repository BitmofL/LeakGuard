using System.Text;

namespace LeakGuard.Core.Services;

/// <summary>
/// Простой локальный сервис логирования без персональных данных.
/// </summary>
public class LoggingService : IDisposable
{
    private readonly string _logDirectory;
    private readonly object _lock = new();
    private bool _enabled = true;
    private bool _disposed;

    public LoggingService()
    {
        _logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LeakGuard", "Logs");

        Directory.CreateDirectory(_logDirectory);
    }

    public bool IsEnabled => _enabled;

    public void Enable() => _enabled = true;
    public void Disable() => _enabled = false;

    public void LogInformation(string message, params object?[] args)
    {
        Log(LogLevel.Info, message, args);
    }

    public void LogWarning(string message, params object?[] args)
    {
        Log(LogLevel.Warn, message, args);
    }

    public void LogError(string message, Exception? ex = null)
    {
        var text = ex != null ? $"{message}: {ex.Message}" : message;
        Log(LogLevel.Error, text, Array.Empty<object?>());
    }

    public void LogDebug(string message, params object?[] args)
    {
        Log(LogLevel.Debug, message, args);
    }

    /// <summary>
    /// Очищает логи старше указанного количества дней.
    /// </summary>
    public void CleanupLogs(int daysToKeep)
    {
        var cutoff = DateTime.Now.AddDays(-daysToKeep);
        var logFiles = Directory.GetFiles(_logDirectory, "*.log");

        foreach (var file in logFiles)
        {
            try
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTime < cutoff)
                    File.Delete(file);
            }
            catch
            {
                // Пропускаем
            }
        }
    }

    public List<string> GetLogFiles()
    {
        return Directory.GetFiles(_logDirectory, "*.log")
            .OrderByDescending(f => new FileInfo(f).LastWriteTime)
            .ToList();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }

    private void Log(LogLevel level, string message, params object?[] args)
    {
        if (!_enabled) return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelStr = level switch
        {
            LogLevel.Info => "INFO",
            LogLevel.Warn => " WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Debug => "DEBUG",
            _ => "?????"
        };

        var formatted = args.Length > 0
            ? string.Format(message, args)
            : message;

        // Не пишем содержимое файлов и ПДН в логи
        var logLine = $"[{timestamp}] [{levelStr}] {formatted}\n";

        lock (_lock)
        {
            var fileName = DateTime.Now.ToString("yyyy-MM-dd") + ".log";
            var filePath = Path.Combine(_logDirectory, fileName);

            try
            {
                File.AppendAllText(filePath, logLine, Encoding.UTF8);
            }
            catch
            {
                // Логи не критичны
            }
        }
    }

    private enum LogLevel
    {
        Info,
        Warn,
        Error,
        Debug
    }
}
