using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LeakGuard.Core.Models;

namespace LeakGuard.Core.Services;

/// <summary>
/// Экспортатор отчётов (HTML, JSON, PDF).
/// </summary>
public class ReportExporter
{
    private readonly AppSettings _settings;

    public ReportExporter(AppSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Экспортирует результаты в выбранный формат.
    /// </summary>
    public async Task<string> ExportAsync(
        List<ScanResult> results,
        ReportFormat format,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        return format switch
        {
            ReportFormat.Html => await ExportHtmlAsync(results, outputPath, cancellationToken),
            ReportFormat.Json => await ExportJsonAsync(results, outputPath, cancellationToken),
            ReportFormat.Pdf => await ExportPdfAsync(results, outputPath, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    private async Task<string> ExportHtmlAsync(
        List<ScanResult> results,
        string outputPath,
        CancellationToken cancellationToken)
    {
        var html = GenerateHtmlReport(results);
        await File.WriteAllTextAsync(outputPath, html, Encoding.UTF8, cancellationToken);
        return outputPath;
    }

    private async Task<string> ExportJsonAsync(
        List<ScanResult> results,
        string outputPath,
        CancellationToken cancellationToken)
    {
        var report = new
        {
            ExportDate = DateTime.UtcNow.ToString("o"),
            Application = "LeakGuard",
            Version = "0.1.0",
            WindowsVersion = GetWindowsVersion(),
            Summary = new
            {
                TotalResults = results.Count,
                Info = results.Count(r => r.RiskLevel == RiskLevel.Info),
                Low = results.Count(r => r.RiskLevel == RiskLevel.Low),
                Medium = results.Count(r => r.RiskLevel == RiskLevel.Medium),
                High = results.Count(r => r.RiskLevel == RiskLevel.High)
            },
            Results = results.Select(r => new
            {
                r.Id,
                FilePath = _settings.IncludeFilePathsInReport ? r.FilePath : DataMasker.MaskPath(r.FilePath),
                Category = r.CategoryDisplay,
                RiskLevel = r.RiskLevelDisplay,
                Reason = r.Reason,
                Confidence = r.Confidence,
                Recommendation = r.Recommendation,
                MaskedExample = _settings.IncludeMaskedExamplesInReport ? r.MaskedExample : null,
                TechnicalInfo = _settings.IncludeTechnicalInfoInReport ? r.TechnicalInfo : null,
                FileType = r.FileType,
                FileModifiedDate = r.FileModifiedDate?.ToString("yyyy-MM-dd"),
                FileSizeBytes = r.FileSize
            })
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var json = JsonSerializer.Serialize(report, options);
        await File.WriteAllTextAsync(outputPath, json, Encoding.UTF8, cancellationToken);
        return outputPath;
    }

    private async Task<string> ExportPdfAsync(
        List<ScanResult> results,
        string outputPath,
        CancellationToken cancellationToken)
    {
        // Простой PDF через HTML-конвертацию (в MVP — заглушка, используем HTML)
        // В полной версии можно использовать PdfSharpCore
        var html = GenerateHtmlReport(results);
        await File.WriteAllTextAsync(outputPath, html, Encoding.UTF8, cancellationToken);
        return outputPath;
    }

    private string GenerateHtmlReport(List<ScanResult> results)
    {
        var html = new StringBuilder();
        html.Append(@"<!DOCTYPE html>
<html lang=""ru"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>LeakGuard — Отчёт сканирования</title>
<style>
  * { margin: 0; padding: 0; box-sizing: border-box; }
  body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #1e1e2e; color: #cdd6f4; padding: 2rem; line-height: 1.6; }
  .container { max-width: 960px; margin: 0 auto; }
  h1 { color: #a6e3a1; margin-bottom: 0.5rem; font-size: 2rem; }
  h2 { color: #a6e3a1; margin: 1.5rem 0 0.75rem; font-size: 1.3rem; border-bottom: 1px solid #45475a; padding-bottom: 0.25rem; }
  .meta { color: #6c7086; font-size: 0.9rem; margin-bottom: 2rem; }
  .summary { display: flex; gap: 1rem; flex-wrap: wrap; margin: 1rem 0; }
  .summary-card { background: #313244; border-radius: 8px; padding: 1rem 1.5rem; min-width: 140px; text-align: center; }
  .summary-card .count { font-size: 2rem; font-weight: bold; }
  .summary-card .label { font-size: 0.85rem; color: #a6adc8; }
  .risk-info .count { color: #89b4fa; }
  .risk-low .count { color: #a6e3a1; }
  .risk-medium .count { color: #f9e2af; }
  .risk-high .count { color: #f38ba8; }
  .result-card { background: #313244; border-radius: 8px; padding: 1rem 1.25rem; margin: 0.75rem 0; border-left: 4px solid #45475a; }
  .result-card.risk-low { border-left-color: #a6e3a1; }
  .result-card.risk-medium { border-left-color: #f9e2af; }
  .result-card.risk-high { border-left-color: #f38ba8; }
  .result-card.risk-info { border-left-color: #89b4fa; }
  .result-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.5rem; }
  .result-category { font-weight: bold; color: #cdd6f4; }
  .result-risk { padding: 0.15rem 0.6rem; border-radius: 4px; font-size: 0.8rem; font-weight: bold; }
  .result-risk.low { background: #3a5a3a; color: #a6e3a1; }
  .result-risk.medium { background: #5a4a2a; color: #f9e2af; }
  .result-risk.high { background: #5a2a2a; color: #f38ba8; }
  .result-risk.info { background: #2a3a5a; color: #89b4fa; }
  .result-reason { color: #bac2de; margin-bottom: 0.35rem; }
  .result-recommendation { color: #a6adc8; font-style: italic; font-size: 0.9rem; }
  .result-meta { color: #6c7086; font-size: 0.8rem; margin-top: 0.35rem; }
  .masked-example { background: #181825; padding: 0.5rem; border-radius: 4px; font-family: monospace; font-size: 0.85rem; color: #a6adc8; margin: 0.5rem 0; white-space: pre-wrap; word-break: break-all; }
  .footer { margin-top: 2rem; padding-top: 1rem; border-top: 1px solid #45475a; color: #6c7086; font-size: 0.85rem; text-align: center; }
  .no-results { text-align: center; padding: 3rem; color: #a6e3a1; font-size: 1.2rem; }
</style>
</head>
<body>
<div class=""container"">
<h1>LeakGuard — Отчёт сканирования</h1>
<div class=""meta"">Дата: {DATE} | Приложение: LeakGuard v{VERSION} | ОС: {OS}</div>
");

        // Сводка
        var info = results.Count(r => r.RiskLevel == RiskLevel.Info);
        var low = results.Count(r => r.RiskLevel == RiskLevel.Low);
        var medium = results.Count(r => r.RiskLevel == RiskLevel.Medium);
        var high = results.Count(r => r.RiskLevel == RiskLevel.High);

        html.Append(@"<h2>Сводка</h2>
<div class=""summary"">
<div class=""summary-card risk-info""><div class=""count"">{INFO}</div><div class=""label"">Информация</div></div>
<div class=""summary-card risk-low""><div class=""count"">{LOW}</div><div class=""label"">Низкий риск</div></div>
<div class=""summary-card risk-medium""><div class=""count"">{MEDIUM}</div><div class=""label"">Средний риск</div></div>
<div class=""summary-card risk-high""><div class=""count"">{HIGH}</div><div class=""label"">Высокий риск</div></div>
</div>
");

        html = html.Replace("{INFO}", info.ToString())
                    .Replace("{LOW}", low.ToString())
                    .Replace("{MEDIUM}", medium.ToString())
                    .Replace("{HIGH}", high.ToString())
                    .Replace("{DATE}", DateTime.Now.ToString("dd.MM.yyyy HH:mm"))
                    .Replace("{VERSION}", "0.1.0")
                    .Replace("{OS}", GetWindowsVersion());

        // Результаты
        if (results.Any())
        {
            html.Append("<h2>Результаты</h2>\n");
            foreach (var r in results.OrderBy(x => (int)x.RiskLevel).ThenBy(x => x.Category))
            {
                var riskClass = r.RiskLevel.ToString().ToLower();
                var riskLabel = r.RiskLevelDisplay.ToLower().Replace(' ', '-');

                html.Append($@"<div class=""result-card risk-{riskClass}"">
<div class=""result-header"">
<span class=""result-category"">{r.CategoryDisplay}</span>
<span class=""result-risk {riskLabel}"">{r.RiskLevelDisplay}</span>
</div>
<div class=""result-reason"">{EscapeHtml(r.Reason)}</div>");

                if (!_settings.IncludeFilePathsInReport)
                {
                    html.Append($@"<div class=""result-meta"">Путь: {EscapeHtml(DataMasker.MaskPath(r.FilePath))}</div>");
                }
                else
                {
                    html.Append($@"<div class=""result-meta"">Путь: {EscapeHtml(r.FilePath)}</div>");
                }

                if (!string.IsNullOrEmpty(r.FileType))
                {
                    html.Append($@"<div class=""result-meta"">Тип: {EscapeHtml(r.FileType)} | Размер: {FormatFileSize(r.FileSize ?? 0)}</div>");
                }

                if (!string.IsNullOrEmpty(r.MaskedExample) && _settings.IncludeMaskedExamplesInReport)
                {
                    html.Append($@"<div class=""masked-example"">{EscapeHtml(r.MaskedExample)}</div>");
                }

                if (!string.IsNullOrEmpty(r.TechnicalInfo) && _settings.IncludeTechnicalInfoInReport)
                {
                    html.Append($@"<div class=""result-meta"">{EscapeHtml(r.TechnicalInfo)}</div>");
                }

                html.Append($@"<div class=""result-recommendation"">{EscapeHtml(r.Recommendation)}</div>
<div class=""result-meta"">Уверенность: {r.Confidence:P0}</div>
</div>
");
            }
        }
        else
        {
            html.Append(@"<div class=""no-results"">Проблем не обнаружено. Ваш компьютер в порядке!</div>");
        }

        html.Append(@"<div class=""footer"">
<p>Отчёт создан приложением LeakGuard v0.1.0</p>
<p>Разработчик: BitmofL | GitHub: <a href=""https://github.com/BitmofL"" style=""color:#89b4fa"" target=""_blank"">github.com/BitmofL</a></p>
<p>Данные в отчёте могут быть замаскированы в соответствии с настройками приватности.</p>
</div>
</div>
</body>
</html>");

        return html.ToString();
    }

    private string EscapeHtml(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = ["Б", "КБ", "МБ", "ГБ"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            len /= 1024;
            order++;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private string GetWindowsVersion()
    {
        try
        {
            var version = Environment.OSVersion.Version;
            return $"Windows {version.Major}.{version.Minor} (Build {version.Build})";
        }
        catch
        {
            return "Windows";
        }
    }
}
