using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LeakGuard.Core.Models;

namespace LeakGuard.Core.Services;

/// <summary>
/// Хранилище отчётов и настроек с поддержкой DPAPI шифрования.
/// </summary>
public class ReportStorage
{
    private readonly string _reportDirectory;
    private readonly string _settingsPath;
    private readonly AppSettings _settings;

    public ReportStorage(AppSettings settings)
    {
        _settings = settings;
        _reportDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LeakGuard", "Reports");

        _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LeakGuard", "settings.json");

        Directory.CreateDirectory(_reportDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
    }

    /// <summary>
    /// Сохраняет отчёт в выбранном формате.
    /// </summary>
    public async Task<string> SaveReportAsync(
        List<ScanResult> results,
        ReportFormat format,
        string? customPath = null)
    {
        var exporter = new ReportExporter(_settings);

        if (!string.IsNullOrEmpty(customPath))
        {
            return await exporter.ExportAsync(results, format, customPath);
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var extension = format switch
        {
            ReportFormat.Html => ".html",
            ReportFormat.Json => ".json",
            ReportFormat.Pdf => ".json", // PDF пока как HTML
            _ => ".html"
        };

        var fileName = $"leakguard_report_{timestamp}{extension}";
        var outputPath = Path.Combine(_reportDirectory, fileName);

        outputPath = await exporter.ExportAsync(results, format, outputPath);

        // Шифрование при необходимости
        if (_settings.EncryptReports)
        {
            await EncryptFileAsync(outputPath);
            var encryptedPath = outputPath + ".encrypted";
            File.Move(outputPath, encryptedPath, true);
            outputPath = encryptedPath;
        }

        // Планируем автоудаление
        ScheduleAutoDelete(outputPath);

        return outputPath;
    }

    /// <summary>
    /// Загружает список сохранённых отчётов.
    /// </summary>
    public List<string> GetReportFiles()
    {
        if (!Directory.Exists(_reportDirectory))
            return new List<string>();

        return Directory.GetFiles(_reportDirectory, "leakguard_report_*")
            .OrderByDescending(f => new FileInfo(f).LastWriteTime)
            .ToList();
    }

    /// <summary>
    /// Удаляет конкретный отчёт.
    /// </summary>
    public void DeleteReport(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            // Удаляем и зашифрованную версию
            var encrypted = filePath + ".encrypted";
            if (File.Exists(encrypted))
                File.Delete(encrypted);
        }
    }

    /// <summary>
    /// Удаляет все отчёты и кэш.
    /// </summary>
    public void DeleteAllReports()
    {
        if (Directory.Exists(_reportDirectory))
        {
            Directory.Delete(_reportDirectory, true);
            Directory.CreateDirectory(_reportDirectory);
        }
    }

    /// <summary>
    /// Сохраняет настройки.
    /// </summary>
    public async Task SaveSettingsAsync()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        var json = JsonSerializer.Serialize(_settings, options);
        await File.WriteAllTextAsync(_settingsPath, json, Encoding.UTF8);
    }

    /// <summary>
    /// Загружает настройки.
    /// </summary>
    public static AppSettings LoadSettings()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LeakGuard", "settings.json");

        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings ?? new AppSettings();
            }
            catch
            {
                // Если не удалось загрузить — используем настройки по умолчанию
            }
        }

        return new AppSettings();
    }

    private async Task EncryptFileAsync(string filePath)
    {
        var plaintext = await File.ReadAllBytesAsync(filePath);
        var encrypted = ProtectedData.Protect(
            plaintext,
            null,
            DataProtectionScope.CurrentUser);

        await File.WriteAllBytesAsync(filePath + ".encrypted", encrypted);
        // Удаляем незашифрованный файл
        File.Delete(filePath);
    }

    private void ScheduleAutoDelete(string filePath)
    {
        if (_settings.AutoDeleteReportDays <= 0)
            return;

        // Создаём файл-маркер для автоудаления
        var markerPath = filePath + ".delete_marker";
        var deleteDate = DateTime.Now.AddDays(_settings.AutoDeleteReportDays);
        File.WriteAllText(markerPath, deleteDate.ToString("o"), Encoding.UTF8);
    }

    /// <summary>
    /// Проверяет и удаляет просроченные отчёты.
    /// </summary>
    public void CleanupExpiredReports()
    {
        if (_settings.AutoDeleteReportDays <= 0)
            return;

        var markerFiles = Directory.GetFiles(_reportDirectory, "*.delete_marker");
        var now = DateTime.Now;

        foreach (var marker in markerFiles)
        {
            try
            {
                var deleteDateStr = File.ReadAllText(marker);
                if (DateTime.TryParse(deleteDateStr, out var deleteDate) && now >= deleteDate)
                {
                    var reportFile = marker.Replace(".delete_marker", "");
                    DeleteReport(reportFile);
                    File.Delete(marker);
                }
            }
            catch
            {
                // Пропускаем повреждённые маркеры
            }
        }
    }
}
