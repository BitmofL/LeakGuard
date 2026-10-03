namespace LeakGuard.Core.Models;

/// <summary>
/// Режим сканирования.
/// </summary>
public enum ScanMode
{
    Quick,
    Full,
    Custom
}

/// <summary>
/// Формат отчёта.
/// </summary>
public enum ReportFormat
{
    Html,
    Json,
    Pdf
}

/// <summary>
/// Область сканирования.
/// </summary>
public class ScanScope
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsSystemFolder { get; set; }
    public List<string> IncludedExtensions { get; set; } = new();
    public List<string> ExcludedFolders { get; set; } = new();
    public long? MaxFileSizeBytes { get; set; }
}
